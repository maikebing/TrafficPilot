using System.Net;
using System.Net.Sockets;
using System.Text;

namespace TrafficPilot;

// ════════════════════════════════════════════════════════════════
//  TCP Relay Server
// ════════════════════════════════════════════════════════════════

internal sealed class TcpRelayServer
{
	private readonly IPAddress _proxyIp;
	private readonly ushort _proxyPort;
	private readonly string _scheme;
	private readonly RedirectNatTable _nat;
	private readonly ConnectionInfoCache _connCache;
	private TcpListener? _listener;
	private readonly CancellationTokenSource _cts = new();

	public long ProxiedSuccess;
	public long ProxiedFailed;

	public TcpRelayServer(IPAddress proxyIp, ushort proxyPort, string scheme,
		RedirectNatTable nat, ConnectionInfoCache connCache)
	{
		_proxyIp = proxyIp;
		_proxyPort = proxyPort;
		_scheme = scheme;
		_nat = nat;
		_connCache = connCache;
	}

	public int Start()
	{
		_listener = new TcpListener(IPAddress.Any, 0);
		_listener.Start(256);
		var port = ((IPEndPoint)_listener.LocalEndpoint).Port;
		_ = AcceptLoopAsync(_cts.Token);
		return port;
	}

	public void Stop()
	{
		_cts.Cancel();
		_listener?.Stop();
	}

	private async Task AcceptLoopAsync(CancellationToken ct)
	{
		while (!ct.IsCancellationRequested)
		{
			try
			{
				var client = await _listener!.AcceptTcpClientAsync(ct);
				_ = HandleClientAsync(client, ct);
			}
			catch (OperationCanceledException) { break; }
			catch { await Task.Delay(50, ct); }
		}
	}

	private async Task HandleClientAsync(TcpClient client, CancellationToken ct)
	{
		TcpClient? proxy = null;
		try
		{
			client.NoDelay = true;
			var ep = (IPEndPoint)client.Client.RemoteEndPoint!;
			var clientAddrBytes = ep.Address.GetAddressBytes();
			var clientPort = (ushort)ep.Port;

			if (!_nat.TryGetOrigDest(clientAddrBytes, clientPort, out var origDstAddr, out var origDstPort))
			{
				client.Close();
				return;
			}

			var clientStream = client.GetStream();
			var firstBuf = new byte[8192];
			int firstLen = 0;
			using (var readCts = CancellationTokenSource.CreateLinkedTokenSource(ct))
			{
				readCts.CancelAfter(2000);
				try { firstLen = await clientStream.ReadAsync(firstBuf, readCts.Token); }
				catch { }
			}

			string? domain = null;
			if (firstLen > 0)
				domain = PayloadExtractor.ExtractDomain(firstBuf, firstLen);

			if (domain is not null)
			{
				var key = (ToU32(clientAddrBytes), clientPort, ToU32(origDstAddr), origDstPort);
				_connCache.Record(key, domain.Contains(' ') ? domain : $"TLS {domain}");
			}

			proxy = new TcpClient { NoDelay = true };
			await proxy.ConnectAsync(_proxyIp, _proxyPort, ct);
			var proxyStream = proxy.GetStream();

			if (_scheme == "socks5")
				await Socks5ConnectAsync(proxyStream, domain, origDstAddr, origDstPort, ct);
			else if (_scheme == "socks4")
				await Socks4ConnectAsync(proxyStream, domain, origDstAddr, origDstPort, ct);
			else
				await HttpConnectAsync(proxyStream, domain, origDstAddr, origDstPort, ct);

			Interlocked.Increment(ref ProxiedSuccess);

			if (firstLen > 0)
				await proxyStream.WriteAsync(firstBuf.AsMemory(0, firstLen), ct);

			using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
			var t1 = CopyStreamAsync(clientStream, proxyStream, linkedCts.Token);
			var t2 = CopyStreamAsync(proxyStream, clientStream, linkedCts.Token);
			await Task.WhenAny(t1, t2);
			await linkedCts.CancelAsync();
		}
		catch (Exception ex) when (ex is not OperationCanceledException)
		{
			Interlocked.Increment(ref ProxiedFailed);
		}
		finally
		{
			try { client.Close(); } catch { }
			try { proxy?.Close(); } catch { }
		}
	}

	private static async Task Socks5ConnectAsync(NetworkStream s, string? domain,
		byte[] dstIp, ushort dstPort, CancellationToken ct)
	{
		await s.WriteAsync(new byte[] { 0x05, 0x01, 0x00 }, ct);
		var greetResp = new byte[2];
		await ReadExactAsync(s, greetResp, 2, ct);
		if (greetResp[0] != 0x05 || greetResp[1] != 0x00)
			throw new InvalidOperationException($"SOCKS5 greeting rejected: 0x{greetResp[0]:X2} 0x{greetResp[1]:X2}");

		byte[] req;
		if (domain is not null && domain.Length is > 0 and <= 255)
		{
			var domBytes = Encoding.ASCII.GetBytes(domain);
			req = new byte[4 + 1 + domBytes.Length + 2];
			req[0] = 0x05; req[1] = 0x01; req[2] = 0x00; req[3] = 0x03;
			req[4] = (byte)domBytes.Length;
			Buffer.BlockCopy(domBytes, 0, req, 5, domBytes.Length);
			req[^2] = (byte)(dstPort >> 8);
			req[^1] = (byte)(dstPort & 0xFF);
		}
		else
		{
			req = new byte[10];
			req[0] = 0x05; req[1] = 0x01; req[2] = 0x00; req[3] = 0x01;
			Buffer.BlockCopy(dstIp, 0, req, 4, 4);
			req[8] = (byte)(dstPort >> 8);
			req[9] = (byte)(dstPort & 0xFF);
		}
		await s.WriteAsync(req, ct);

		var hdr = new byte[4];
		await ReadExactAsync(s, hdr, 4, ct);
		if (hdr[1] != 0x00)
			throw new InvalidOperationException($"SOCKS5 CONNECT rejected: REP=0x{hdr[1]:X2}");

		int addrLen = hdr[3] switch
		{
			0x01 => 4,
			0x04 => 16,
			0x03 => -1,
			_ => throw new InvalidOperationException($"SOCKS5 unknown ATYP=0x{hdr[3]:X2}")
		};
		if (addrLen == -1)
		{
			var lenBuf = new byte[1];
			await ReadExactAsync(s, lenBuf, 1, ct);
			addrLen = lenBuf[0];
		}
		var tail = new byte[addrLen + 2];
		await ReadExactAsync(s, tail, tail.Length, ct);
	}

	private static async Task Socks4ConnectAsync(NetworkStream s, string? domain,
		byte[] dstIp, ushort dstPort, CancellationToken ct)
	{
		byte[] req;
		if (!string.IsNullOrWhiteSpace(domain))
		{
			var domBytes = Encoding.ASCII.GetBytes(domain);
			req = new byte[8 + 1 + domBytes.Length + 1];
			req[0] = 0x04; req[1] = 0x01;
			req[2] = (byte)(dstPort >> 8);
			req[3] = (byte)(dstPort & 0xFF);
			req[4] = 0x00; req[5] = 0x00; req[6] = 0x00; req[7] = 0x01;
			req[8] = 0x00;
			Buffer.BlockCopy(domBytes, 0, req, 9, domBytes.Length);
			req[^1] = 0x00;
		}
		else
		{
			req = new byte[9];
			req[0] = 0x04; req[1] = 0x01;
			req[2] = (byte)(dstPort >> 8);
			req[3] = (byte)(dstPort & 0xFF);
			Buffer.BlockCopy(dstIp, 0, req, 4, 4);
			req[8] = 0x00;
		}

		await s.WriteAsync(req, ct);
		var resp = new byte[8];
		await ReadExactAsync(s, resp, resp.Length, ct);
		if (resp[1] != 0x5A)
			throw new InvalidOperationException($"SOCKS4 CONNECT rejected: CD=0x{resp[1]:X2}");
	}

	private static async Task HttpConnectAsync(NetworkStream s, string? domain,
		byte[] dstIp, ushort dstPort, CancellationToken ct)
	{
		var host = domain ?? $"{dstIp[0]}.{dstIp[1]}.{dstIp[2]}.{dstIp[3]}";
		var request = $"CONNECT {host}:{dstPort} HTTP/1.1\r\nHost: {host}:{dstPort}\r\n\r\n";
		await s.WriteAsync(Encoding.ASCII.GetBytes(request), ct);

		var buf = new byte[4096];
		int total = 0;
		while (total < buf.Length)
		{
			int n = await s.ReadAsync(buf.AsMemory(total), ct);
			if (n == 0) throw new InvalidOperationException("HTTP CONNECT: connection closed");
			total += n;
			var text = Encoding.ASCII.GetString(buf, 0, total);
			if (text.Contains("\r\n\r\n"))
			{
				if (!text.StartsWith("HTTP/1.1 200") && !text.StartsWith("HTTP/1.0 200"))
					throw new InvalidOperationException($"HTTP CONNECT rejected: {text.Split('\r')[0]}");
				return;
			}
		}
		throw new InvalidOperationException("HTTP CONNECT: response too large");
	}

	private static async Task CopyStreamAsync(NetworkStream from, NetworkStream to, CancellationToken ct)
	{
		var buf = new byte[65536];
		try
		{
			int n;
			while ((n = await from.ReadAsync(buf, ct)) > 0)
				await to.WriteAsync(buf.AsMemory(0, n), ct);
		}
		catch { }
	}

	private static async Task ReadExactAsync(NetworkStream s, byte[] buf, int count, CancellationToken ct)
	{
		int read = 0;
		while (read < count)
		{
			int n = await s.ReadAsync(buf.AsMemory(read, count - read), ct);
			if (n == 0) throw new EndOfStreamException("Unexpected end of stream");
			read += n;
		}
	}

	private static uint ToU32(byte[] a) =>
		(uint)(a[0] | (a[1] << 8) | (a[2] << 16) | (a[3] << 24));
}
