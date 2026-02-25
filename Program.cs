using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Text.RegularExpressions;

// ════════════════════════════════════════════════════════════════
//  Guard checks
// ════════════════════════════════════════════════════════════════

if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
{
	Console.Error.WriteLine("This program only supports Windows.");
	return;
}

var options = ProxyOptions.Parse(args);
if (options is null) { ProxyOptions.PrintUsage(); return; }

var proxyIp = ResolveProxyIpv4(options.ProxyHost);
if (proxyIp.AddressFamily != AddressFamily.InterNetwork)
{
	Console.Error.WriteLine("Only IPv4 proxy address is supported.");
	return;
}

var processMatcher = new ProcessAllowListMatcher(options.ProcessNames, options.ExtraPids, TimeSpan.FromSeconds(1));
if (!processMatcher.HasAnyRule)
{
	Console.Error.WriteLine("No target process rule found. Use --pid, --process, or --process-list.");
	return;
}

var localBypass = LocalTrafficBypass.CreateDefault();
var skipLogDedup = new SkipLogDedup(TimeSpan.FromSeconds(30));

// ════════════════════════════════════════════════════════════════
//  Start local TCP relay (SOCKS5 / HTTP CONNECT → upstream proxy)
// ════════════════════════════════════════════════════════════════

var redirectNat = new RedirectNatTable(TimeSpan.FromMinutes(10));
var connInfoCache = new ConnectionInfoCache(TimeSpan.FromMinutes(5));
var processNameResolver = new ProcessNameResolver(TimeSpan.FromSeconds(3));
var relay = new TcpRelayServer(proxyIp, options.ProxyPort, options.ProxyScheme, redirectNat, connInfoCache);
var relayPort = (ushort)relay.Start();

Console.WriteLine($"Target    : {processMatcher.Describe()}");
Console.WriteLine($"Proxy     : {proxyIp}:{options.ProxyPort} ({options.ProxyScheme})");
Console.WriteLine($"Relay     : 0.0.0.0:{relayPort} (redirect to src IP)");
Console.WriteLine($"Bypass    : {localBypass.DescribeRules()}");
Console.WriteLine($"AddrSize  : {Marshal.SizeOf<WinDivertAddress>()} bytes (expect 80)");
PrintStartupSelfCheck(options, proxyIp);
Console.WriteLine("Press Ctrl+C to stop...");
Console.WriteLine();

// ════════════════════════════════════════════════════════════════
//  Open WinDivert
// ════════════════════════════════════════════════════════════════

var handle = WinDivertNative.WinDivertOpen("ip and tcp", WinDivertLayer.Network, 0, 0UL);
if (handle == IntPtr.Zero || handle == new IntPtr(-1))
{
	var err = Marshal.GetLastWin32Error();
	throw new Win32Exception(err,
		$"WinDivertOpen failed (err={err}). Ensure WinDivert.dll/WinDivert64.sys present and running as admin.");
}

var tcpTableResolver = new TcpOwnerPidResolver(TimeSpan.FromMilliseconds(500));
var stats = new RedirectStats();
var selfPid = Environment.ProcessId;
int bootLogRemaining = 40;

Console.CancelKeyPress += (_, e) =>
{
	e.Cancel = true;
	Console.WriteLine("\nShutting down...");
	relay.Stop();
	tcpTableResolver.Dispose();
	processMatcher.Dispose();
	processNameResolver.Dispose();
	redirectNat.Dispose();
	connInfoCache.Dispose();
	WinDivertNative.WinDivertClose(handle);
	Environment.Exit(0);
};

// ════════════════════════════════════════════════════════════════
//  Main packet processing loop
// ════════════════════════════════════════════════════════════════

var packet = new byte[0xFFFF];

while (true)
{
	var addr = new WinDivertAddress();
	if (!WinDivertNative.WinDivertRecv(handle, packet, (uint)packet.Length, out var recvLen, ref addr))
		continue;

	stats.ReceivedPackets++;

	// ── Boot diagnostic ──
	if (bootLogRemaining > 0)
	{
		bootLogRemaining--;
		Console.WriteLine(
			$"[boot {DateTime.Now:HH:mm:ss.fff}] flags=0x{addr.Flags:X8} " +
			$"outbound={addr.IsOutbound} loopback={addr.IsLoopback} " +
			$"ifIdx={addr.IfIdx} subIfIdx={addr.SubIfIdx}");
	}

	// Parse IP+TCP
	if (!PacketInspector.TryParseIpv4Tcp(packet, (int)recvLen, out var pkt))
	{
		stats.NonIpv4Tcp++;
		WinDivertNative.WinDivertSend(handle, packet, recvLen, out _, ref addr);
		stats.TryPrint();
		continue;
	}

	// ══════════════════════════════════════════════
	//  PATH 1: Reply from local relay → reverse-NAT
	//    src=<localIP>:relayPort → rewrite to original server
	//    Match by relay port (dynamically allocated, unique)
	// ══════════════════════════════════════════════
	if (pkt.SrcPort == relayPort)
	{
		if (redirectNat.TryGetOrigDest(pkt.DstAddr, pkt.DstPort,
				out var origDst, out var origPort))
		{
			PacketInspector.RewriteSource(packet, pkt, origDst, origPort);
			WinDivertNative.WinDivertHelperCalcChecksums(packet, recvLen, ref addr, 0UL);
			stats.InboundRewritten++;
		}
		WinDivertNative.WinDivertSend(handle, packet, recvLen, out _, ref addr);
		stats.TryPrint();
		continue;
	}

	// Only process outbound from here
	if (!addr.IsOutbound)
	{
		stats.InboundPackets++;
		WinDivertNative.WinDivertSend(handle, packet, recvLen, out _, ref addr);
		stats.TryPrint();
		continue;
	}

	stats.OutboundPackets++;

	// ══════════════════════════════════════════════
	//  PATH 2: Outbound from target process → redirect to relay
	// ══════════════════════════════════════════════

	// Skip local/private destinations
	if (localBypass.ShouldBypass(pkt.DstAddr))
	{
		stats.LocalBypassed++;
		WinDivertNative.WinDivertSend(handle, packet, recvLen, out _, ref addr);
		stats.TryPrint();
		continue;
	}

	// Resolve PID
	var pid = tcpTableResolver.FindOwnerPid(pkt.SrcAddr, pkt.SrcPort, pkt.DstAddr, pkt.DstPort);

	// Skip our own relay's upstream connections
	if (pid == selfPid)
	{
		WinDivertNative.WinDivertSend(handle, packet, recvLen, out _, ref addr);
		stats.TryPrint();
		continue;
	}

	// Check process allow-list
	if (pid is null || !processMatcher.ContainsPid(pid.Value))
	{
		if (pid is null) stats.PidMiss++;
		else stats.ProcessSkipped++;

		// Diagnostic: log skipped SYN connections (only first per dst, deduped)
		bool isSyn = PacketInspector.IsSyn(packet, pkt);
		if (isSyn)
		{
			var dstIpStr = FormatIp(pkt.DstAddr);
			if (skipLogDedup.ShouldLog(dstIpStr, pkt.DstPort))
			{
				var skipProcName = pid is not null ? processNameResolver.Resolve(pid.Value) : "?";
				var reason = pid is null ? "PID not found" : "Process not in allow-list";
				Console.ForegroundColor = ConsoleColor.DarkGray;
				Console.WriteLine(
					$"[{DateTime.Now:HH:mm:ss.fff}] [skip] {skipProcName}({pid?.ToString() ?? "?"})  " +
					$"{FormatIp(pkt.SrcAddr)}:{pkt.SrcPort} -> {dstIpStr}:{pkt.DstPort}  ({reason})");
				Console.ResetColor();
			}
		}

		WinDivertNative.WinDivertSend(handle, packet, recvLen, out _, ref addr);
		stats.TryPrint();
		continue;
	}

	// Safety: already going to relay?
	if (pkt.DstPort == relayPort && pkt.DstAddr.AsSpan().SequenceEqual(pkt.SrcAddr))
	{
		stats.AlreadyProxy++;
		WinDivertNative.WinDivertSend(handle, packet, recvLen, out _, ref addr);
		stats.TryPrint();
		continue;
	}

	// ── Extract HTTP/TLS info for logging ──
	var connKey = (ToU32(pkt.SrcAddr), pkt.SrcPort, ToU32(pkt.DstAddr), pkt.DstPort);
	var httpInfo = PayloadExtractor.TryExtract(packet, (int)recvLen, pkt);
	if (httpInfo is not null)
		connInfoCache.Record(connKey, httpInfo);

	// ── Log (only on SYN or when new info is discovered) ──
	bool isSynForLog = PacketInspector.IsSyn(packet, pkt);
	if (isSynForLog || httpInfo is not null)
	{
		var displayInfo = httpInfo ?? connInfoCache.TryGet(connKey);
		var procName = processNameResolver.Resolve(pid.Value);
		var infoTag = displayInfo is not null ? $"  [{displayInfo}]" : "";
		Console.WriteLine(
			$"[{DateTime.Now:HH:mm:ss.fff}] {procName}({pid.Value})  " +
			$"{FormatIp(pkt.SrcAddr)}:{pkt.SrcPort} -> {FormatIp(pkt.DstAddr)}:{pkt.DstPort}  " +
			$">> relay >> {options.ProxyScheme}://{proxyIp}:{options.ProxyPort}{infoTag}");
	}

	// ── Record NAT mapping and redirect to local relay ──
	// Redirect to the source's own IP to avoid cross-interface loopback issues
	redirectNat.RecordRedirect(pkt.SrcAddr, pkt.SrcPort, pkt.DstAddr, pkt.DstPort);
	PacketInspector.RewriteDestination(packet, pkt, pkt.SrcAddr, relayPort);
	WinDivertNative.WinDivertHelperCalcChecksums(packet, recvLen, ref addr, 0UL);
	WinDivertNative.WinDivertSend(handle, packet, recvLen, out _, ref addr);
	stats.Redirected++;
	stats.ProxiedOk = Interlocked.Read(ref relay.ProxiedSuccess);
	stats.ProxiedFail = Interlocked.Read(ref relay.ProxiedFailed);
	stats.TryPrint();
}

// ════════════════════════════════════════════════════════════════
//  Helper methods
// ════════════════════════════════════════════════════════════════

static IPAddress ResolveProxyIpv4(string host)
{
	if (IPAddress.TryParse(host, out var ip)) return ip;
	var addrs = Dns.GetHostAddresses(host);
	return addrs.FirstOrDefault(a => a.AddressFamily == AddressFamily.InterNetwork)
		?? throw new InvalidOperationException($"Cannot resolve IPv4 for '{host}'.");
}

static string FormatIp(byte[] a) => a.Length == 4 ? $"{a[0]}.{a[1]}.{a[2]}.{a[3]}" : "?";
static uint ToU32(byte[] a) => (uint)(a[0] | (a[1] << 8) | (a[2] << 16) | (a[3] << 24));

static void PrintStartupSelfCheck(ProxyOptions options, IPAddress proxyIp)
{
	Console.WriteLine("SelfCheck : starting...");

	// 1) Admin check
	bool isAdmin = false;
	try
	{
		using var identity = WindowsIdentity.GetCurrent();
		var principal = new WindowsPrincipal(identity);
		isAdmin = principal.IsInRole(WindowsBuiltInRole.Administrator);
	}
	catch { }
	Console.WriteLine($"SelfCheck : admin={(isAdmin ? "OK" : "FAIL")}" );

	// 2) TCP connect check
	using var client = new TcpClient();
	try
	{
		var connectTask = client.ConnectAsync(proxyIp, options.ProxyPort);
		if (!connectTask.Wait(TimeSpan.FromSeconds(2)))
			throw new TimeoutException("connect timeout");

		client.ReceiveTimeout = 2000;
		client.SendTimeout = 2000;
		Console.WriteLine($"SelfCheck : tcp {proxyIp}:{options.ProxyPort}=OK");

		// 3) Protocol handshake check
		using var stream = client.GetStream();
		if (options.ProxyScheme == "socks5")
		{
			stream.Write([0x05, 0x01, 0x00]);
			var resp = new byte[2];
			if (!TryReadExact(stream, resp, 2))
				throw new InvalidOperationException("SOCKS5 no response");
			if (resp[0] != 0x05 || resp[1] != 0x00)
				throw new InvalidOperationException($"SOCKS5 reject: {resp[0]:X2} {resp[1]:X2}");
			Console.WriteLine("SelfCheck : socks5 handshake=OK");
		}
		else if (options.ProxyScheme == "socks4")
		{
			stream.Write([0x04, 0x01, 0x01, 0xBB, 0x01, 0x01, 0x01, 0x01, 0x00]);
			var resp = new byte[8];
			if (!TryReadExact(stream, resp, 8))
				throw new InvalidOperationException("SOCKS4 no response");
			if (resp[1] != 0x5A)
				throw new InvalidOperationException($"SOCKS4 reject: CD=0x{resp[1]:X2}");
			Console.WriteLine("SelfCheck : socks4 handshake=OK");
		}
		else
		{
			var req = "CONNECT example.com:443 HTTP/1.1\r\nHost: example.com:443\r\n\r\n";
			var reqBytes = Encoding.ASCII.GetBytes(req);
			stream.Write(reqBytes, 0, reqBytes.Length);

			var buf = new byte[512];
			int n = stream.Read(buf, 0, buf.Length);
			if (n <= 0)
				throw new InvalidOperationException("HTTP CONNECT no response");
			var line = Encoding.ASCII.GetString(buf, 0, n).Split("\r\n", StringSplitOptions.None)[0];
			if (!line.Contains(" 200 "))
				throw new InvalidOperationException($"HTTP CONNECT reject: {line}");
			Console.WriteLine("SelfCheck : http-connect handshake=OK");
		}
	}
	catch (Exception ex)
	{
		Console.ForegroundColor = ConsoleColor.Yellow;
		Console.WriteLine($"SelfCheck : FAIL - {ex.Message}");
		Console.WriteLine("SelfCheck : 若握手失败，请确认端口与协议匹配（7890常见http，7899常见socks/mixed）。");
		Console.ResetColor();
	}
}

static bool TryReadExact(NetworkStream stream, byte[] buffer, int count)
{
	int read = 0;
	while (read < count)
	{
		int n = stream.Read(buffer, read, count - read);
		if (n <= 0) return false;
		read += n;
	}
	return true;
}

// ════════════════════════════════════════════════════════════════
//  TCP Relay Server
//  Accepts redirected connections, connects upstream via
//  SOCKS4/SOCKS5/HTTP CONNECT, and relays data bidirectionally.
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

	// Proxy relay counters
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

			// Look up original destination from NAT table
			if (!_nat.TryGetOrigDest(clientAddrBytes, clientPort,
					out var origDstAddr, out var origDstPort))
			{
				Console.ForegroundColor = ConsoleColor.DarkYellow;
				Console.WriteLine(
					$"[{DateTime.Now:HH:mm:ss.fff}] [relay] NAT lookup failed for " +
					$"{ep.Address}:{ep.Port} — no mapping found");
				Console.ResetColor();
				client.Close();
				return;
			}

			var origDstIpStr = $"{origDstAddr[0]}.{origDstAddr[1]}.{origDstAddr[2]}.{origDstAddr[3]}";
			var clientStream = client.GetStream();

			// Read first data to extract domain name (SNI / Host header)
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

			// Record domain info for logging
			if (domain is not null)
			{
				var key = (ToU32(clientAddrBytes), clientPort,
					ToU32(origDstAddr), origDstPort);
				_connCache.Record(key, domain.Contains(' ') ? domain : $"TLS {domain}");
			}

			// Display target for logging
			var target = domain ?? origDstIpStr;
			var targetDisplay = $"{target}:{origDstPort}";

			// Connect to upstream proxy
			proxy = new TcpClient { NoDelay = true };
			await proxy.ConnectAsync(_proxyIp, _proxyPort, ct);
			var proxyStream = proxy.GetStream();

			// Protocol handshake
			if (_scheme == "socks5")
				await Socks5ConnectAsync(proxyStream, domain, origDstAddr, origDstPort, ct);
			else if (_scheme == "socks4")
				await Socks4ConnectAsync(proxyStream, domain, origDstAddr, origDstPort, ct);
			else
				await HttpConnectAsync(proxyStream, domain, origDstAddr, origDstPort, ct);

			// ── Log successful proxy connection ──
			Interlocked.Increment(ref ProxiedSuccess);
			Console.ForegroundColor = ConsoleColor.Green;
			Console.WriteLine(
				$"[{DateTime.Now:HH:mm:ss.fff}] [proxied] {targetDisplay}  via {_scheme}://{_proxyIp}:{_proxyPort}");
			Console.ResetColor();

			// Forward buffered first data through tunnel
			if (firstLen > 0)
				await proxyStream.WriteAsync(firstBuf.AsMemory(0, firstLen), ct);

			// Bidirectional relay
			using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
			var t1 = CopyStreamAsync(clientStream, proxyStream, linkedCts.Token);
			var t2 = CopyStreamAsync(proxyStream, clientStream, linkedCts.Token);
			await Task.WhenAny(t1, t2);
			await linkedCts.CancelAsync();
		}
		catch (Exception ex) when (ex is not OperationCanceledException)
		{
			Interlocked.Increment(ref ProxiedFailed);
			Console.ForegroundColor = ConsoleColor.Red;
			Console.WriteLine(
				$"[{DateTime.Now:HH:mm:ss.fff}] [proxy-fail] {ex.GetType().Name}: {ex.Message}");
			Console.ResetColor();
		}
		finally
		{
			try { client.Close(); } catch { }
			try { proxy?.Close(); } catch { }
		}
	}

	// ── SOCKS5 handshake ──

	private static async Task Socks5ConnectAsync(NetworkStream s, string? domain,
		byte[] dstIp, ushort dstPort, CancellationToken ct)
	{
		// Greeting: version=5, 1 method (0=no auth)
		await s.WriteAsync(new byte[] { 0x05, 0x01, 0x00 }, ct);
		var greetResp = new byte[2];
		await ReadExactAsync(s, greetResp, 2, ct);
		if (greetResp[0] != 0x05 || greetResp[1] != 0x00)
			throw new InvalidOperationException(
				$"SOCKS5 greeting rejected: 0x{greetResp[0]:X2} 0x{greetResp[1]:X2}");

		// CONNECT request
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

		// Response: VER(1) REP(1) RSV(1) ATYP(1) BND.ADDR(var) BND.PORT(2)
		var hdr = new byte[4];
		await ReadExactAsync(s, hdr, 4, ct);
		if (hdr[1] != 0x00)
			throw new InvalidOperationException($"SOCKS5 CONNECT rejected: REP=0x{hdr[1]:X2}");

		// Consume BND.ADDR + BND.PORT
		int addrLen = hdr[3] switch
		{
			0x01 => 4,
			0x04 => 16,
			0x03 => -1, // need to read length byte
			_ => throw new InvalidOperationException($"SOCKS5 unknown ATYP=0x{hdr[3]:X2}")
		};
		if (addrLen == -1)
		{
			var lenBuf = new byte[1];
			await ReadExactAsync(s, lenBuf, 1, ct);
			addrLen = lenBuf[0];
		}
		var tail = new byte[addrLen + 2]; // addr + port
		await ReadExactAsync(s, tail, tail.Length, ct);
	}

	// ── HTTP CONNECT handshake ──

	private static async Task Socks4ConnectAsync(NetworkStream s, string? domain,
		byte[] dstIp, ushort dstPort, CancellationToken ct)
	{
		byte[] req;
		if (!string.IsNullOrWhiteSpace(domain))
		{
			var domBytes = Encoding.ASCII.GetBytes(domain);
			req = new byte[8 + 1 + domBytes.Length + 1];
			req[0] = 0x04;
			req[1] = 0x01;
			req[2] = (byte)(dstPort >> 8);
			req[3] = (byte)(dstPort & 0xFF);
			req[4] = 0x00;
			req[5] = 0x00;
			req[6] = 0x00;
			req[7] = 0x01;
			req[8] = 0x00;
			Buffer.BlockCopy(domBytes, 0, req, 9, domBytes.Length);
			req[^1] = 0x00;
		}
		else
		{
			req = new byte[9];
			req[0] = 0x04;
			req[1] = 0x01;
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

		// Read until \r\n\r\n
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

	// ── Stream helpers ──

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

// ════════════════════════════════════════════════════════════════
//  WinDivert P/Invoke — CRITICAL: struct must be exactly 80 bytes
// ════════════════════════════════════════════════════════════════

internal enum WinDivertLayer : uint { Network = 0 }

[StructLayout(LayoutKind.Explicit, Size = 80)]
internal struct WinDivertAddress
{
	[FieldOffset(0)] public long Timestamp;
	[FieldOffset(8)] public uint Flags;
	[FieldOffset(12)] public uint Reserved;
	[FieldOffset(16)] public uint IfIdx;
	[FieldOffset(20)] public uint SubIfIdx;

	public readonly byte Layer => (byte)(Flags & 0xFFu);
	public readonly byte Event => (byte)((Flags >> 8) & 0xFFu);
	public readonly bool IsOutbound => (Flags & (1u << 17)) != 0;
	public readonly bool IsLoopback => (Flags & (1u << 18)) != 0;
}

internal static class WinDivertNative
{
	private const string Dll = "WinDivert.dll";

	[DllImport(Dll, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, SetLastError = true)]
	public static extern IntPtr WinDivertOpen(
		[MarshalAs(UnmanagedType.LPStr)] string filter,
		WinDivertLayer layer, short priority, ulong flags);

	[DllImport(Dll, CallingConvention = CallingConvention.Cdecl, SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	public static extern bool WinDivertRecv(
		IntPtr handle, byte[] pPacket, uint packetLen,
		out uint readLen, ref WinDivertAddress pAddr);

	[DllImport(Dll, CallingConvention = CallingConvention.Cdecl, SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	public static extern bool WinDivertSend(
		IntPtr handle, byte[] pPacket, uint packetLen,
		out uint writeLen, ref WinDivertAddress pAddr);

	[DllImport(Dll, CallingConvention = CallingConvention.Cdecl, SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	public static extern bool WinDivertClose(IntPtr handle);

	[DllImport(Dll, CallingConvention = CallingConvention.Cdecl, SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	public static extern bool WinDivertHelperCalcChecksums(
		byte[] pPacket, uint packetLen, ref WinDivertAddress pAddr, ulong flags);
}

// ════════════════════════════════════════════════════════════════
//  Packet Inspector
// ════════════════════════════════════════════════════════════════

internal readonly record struct ParsedPacket(
	byte[] SrcAddr, ushort SrcPort,
	byte[] DstAddr, ushort DstPort,
	int IpHeaderLen);

internal static class PacketInspector
{
	public static bool TryParseIpv4Tcp(byte[] buf, int len, out ParsedPacket pkt)
	{
		pkt = default;
		if (len < 40) return false; // min IP(20) + TCP(20)
		if (((buf[0] >> 4) & 0xF) != 4) return false;
		int ihl = (buf[0] & 0xF) * 4;
		if (ihl < 20 || len < ihl + 20) return false;
		if (buf[9] != 6) return false; // TCP

		var src = new byte[4]; Buffer.BlockCopy(buf, 12, src, 0, 4);
		var dst = new byte[4]; Buffer.BlockCopy(buf, 16, dst, 0, 4);
		ushort srcPort = (ushort)((buf[ihl] << 8) | buf[ihl + 1]);
		ushort dstPort = (ushort)((buf[ihl + 2] << 8) | buf[ihl + 3]);

		pkt = new ParsedPacket(src, srcPort, dst, dstPort, ihl);
		return true;
	}

	public static void RewriteDestination(byte[] buf, ParsedPacket pkt, byte[] newDst, ushort newPort)
	{
		Buffer.BlockCopy(newDst, 0, buf, 16, 4);
		buf[pkt.IpHeaderLen + 2] = (byte)(newPort >> 8);
		buf[pkt.IpHeaderLen + 3] = (byte)(newPort & 0xFF);
	}

	public static void RewriteSource(byte[] buf, ParsedPacket pkt, byte[] newSrc, ushort newPort)
	{
		Buffer.BlockCopy(newSrc, 0, buf, 12, 4);
		buf[pkt.IpHeaderLen] = (byte)(newPort >> 8);
		buf[pkt.IpHeaderLen + 1] = (byte)(newPort & 0xFF);
	}

	/// <summary>TCP SYN flag set, ACK not set (new connection initiation)</summary>
	public static bool IsSyn(byte[] buf, ParsedPacket pkt)
	{
		int flagsOffset = pkt.IpHeaderLen + 13;
		if (flagsOffset >= buf.Length) return false;
		byte flags = buf[flagsOffset];
		return (flags & 0x02) != 0 && (flags & 0x10) == 0; // SYN=1, ACK=0
	}
}

// ════════════════════════════════════════════════════════════════
//  Payload Extractor (HTTP / TLS)
// ════════════════════════════════════════════════════════════════

internal static class PayloadExtractor
{
	/// <summary>
	/// Extract info from a WinDivert IP+TCP packet for logging.
	/// Returns e.g. "GET http://host/path" or "TLS example.com".
	/// </summary>
	public static string? TryExtract(byte[] buf, int totalLen, ParsedPacket pkt)
	{
		int tcpOff = pkt.IpHeaderLen;
		if (totalLen < tcpOff + 20) return null;
		int dataOff = tcpOff + (((buf[tcpOff + 12] >> 4) & 0xF) * 4);
		int payloadLen = totalLen - dataOff;
		if (payloadLen <= 0) return null;

		if (buf[dataOff] == 0x16 && payloadLen > 5)
		{
			var sni = TryParseTlsSni(buf, dataOff, payloadLen);
			if (sni is not null) return $"TLS {sni}";
		}

		var http = TryParseHttpRequestInfo(buf, dataOff, payloadLen);
		return http;
	}

	/// <summary>
	/// Extract just the domain name from raw TCP payload (for relay SOCKS5 CONNECT).
	/// </summary>
	public static string? ExtractDomain(byte[] payload, int len)
	{
		if (len <= 0) return null;

		if (payload[0] == 0x16 && len > 5)
		{
			var sni = TryParseTlsSni(payload, 0, len);
			if (sni is not null) return sni;
		}

		return TryParseHttpHostOnly(payload, 0, len);
	}

	// ── HTTP request line + Host header (for logging) ──

	private static string? TryParseHttpRequestInfo(byte[] buf, int offset, int len)
	{
		ReadOnlySpan<byte> data = buf.AsSpan(offset, Math.Min(len, 4096));
		int lineEnd = data.IndexOf((byte)'\n');
		if (lineEnd < 0) lineEnd = data.Length;
		if (lineEnd > 0 && data[lineEnd - 1] == '\r') lineEnd--;
		if (lineEnd < 10) return null;

		var firstLine = data[..lineEnd];
		if (firstLine.IndexOf("HTTP/"u8) < 0) return null;

		int sp1 = firstLine.IndexOf((byte)' ');
		if (sp1 < 0 || sp1 > 10) return null;
		var method = Encoding.ASCII.GetString(firstLine[..sp1]);

		var afterMethod = firstLine[(sp1 + 1)..];
		int sp2 = afterMethod.IndexOf((byte)' ');
		var uri = sp2 > 0
			? Encoding.ASCII.GetString(afterMethod[..sp2])
			: Encoding.ASCII.GetString(afterMethod);

		var host = FindHostHeader(data);

		if (uri.StartsWith("http://") || uri.StartsWith("https://"))
			return $"{method} {uri}";
		if (host is not null)
			return $"{method} http://{host}{uri}";
		return $"{method} {uri}";
	}

	// ── HTTP Host header only (for domain extraction) ──

	private static string? TryParseHttpHostOnly(byte[] buf, int offset, int len)
	{
		ReadOnlySpan<byte> data = buf.AsSpan(offset, Math.Min(len, 4096));

		// Verify it looks like HTTP
		if (data.IndexOf("HTTP/"u8) < 0) return null;

		var host = FindHostHeader(data);
		if (host is null) return null;

		// Strip port if present (e.g., "example.com:443" → "example.com")
		int colon = host.LastIndexOf(':');
		if (colon > 0 && !host.Contains('[')) // avoid stripping IPv6
			host = host[..colon];

		return host.Length > 0 ? host : null;
	}

	private static string? FindHostHeader(ReadOnlySpan<byte> data)
	{
		ReadOnlySpan<byte> hostPrefix = "Host: "u8;
		ReadOnlySpan<byte> hostLower = "host: "u8;
		int searchStart = 0;
		while (searchStart < data.Length - 6)
		{
			int nl = data[searchStart..].IndexOf((byte)'\n');
			if (nl < 0) break;
			int headerStart = searchStart + nl + 1;
			if (headerStart >= data.Length) break;

			var remaining = data[headerStart..];
			if (remaining.Length >= hostPrefix.Length &&
				(remaining.StartsWith(hostPrefix) || remaining.StartsWith(hostLower)))
			{
				var afterHost = remaining[hostPrefix.Length..];
				int end = afterHost.IndexOf((byte)'\r');
				if (end < 0) end = afterHost.IndexOf((byte)'\n');
				if (end < 0) end = Math.Min(afterHost.Length, 256);
				return Encoding.ASCII.GetString(afterHost[..end]).Trim();
			}
			searchStart = headerStart;
		}
		return null;
	}

	// ── TLS ClientHello SNI extraction ──

	private static string? TryParseTlsSni(byte[] buf, int offset, int len)
	{
		if (len < 6) return null;
		int hsStart = offset + 5;
		int hsLen = (buf[offset + 3] << 8) | buf[offset + 4];
		if (hsLen < 1) return null;
		int hsEnd = Math.Min(offset + 5 + hsLen, offset + len);

		if (hsStart >= hsEnd || buf[hsStart] != 0x01) return null; // ClientHello

		int pos = hsStart + 1 + 3 + 2 + 32; // type + len3 + ver2 + random32
		if (pos >= hsEnd) return null;

		int sidLen = buf[pos]; pos++; pos += sidLen;
		if (pos + 2 >= hsEnd) return null;

		int csLen = (buf[pos] << 8) | buf[pos + 1]; pos += 2; pos += csLen;
		if (pos + 1 >= hsEnd) return null;

		int cmLen = buf[pos]; pos++; pos += cmLen;
		if (pos + 2 >= hsEnd) return null;

		int extTotalLen = (buf[pos] << 8) | buf[pos + 1]; pos += 2;
		int extEnd = Math.Min(pos + extTotalLen, hsEnd);

		while (pos + 4 <= extEnd)
		{
			int extType = (buf[pos] << 8) | buf[pos + 1]; pos += 2;
			int extLen = (buf[pos] << 8) | buf[pos + 1]; pos += 2;
			if (pos + extLen > extEnd) break;

			if (extType == 0x0000) // SNI
				return ParseSniList(buf, pos, extLen);
			pos += extLen;
		}
		return null;
	}

	private static string? ParseSniList(byte[] buf, int offset, int extLen)
	{
		if (extLen < 5) return null;
		int listLen = (buf[offset] << 8) | buf[offset + 1];
		int pos = offset + 2;
		int listEnd = Math.Min(offset + 2 + listLen, offset + extLen);

		while (pos + 3 <= listEnd)
		{
			int nameType = buf[pos]; pos++;
			int nameLen = (buf[pos] << 8) | buf[pos + 1]; pos += 2;
			if (nameType == 0 && nameLen > 0 && pos + nameLen <= listEnd)
				return Encoding.ASCII.GetString(buf, pos, nameLen);
			pos += nameLen;
		}
		return null;
	}
}

// ════════════════════════════════════════════════════════════════
//  Redirect NAT Table
//  Maps (clientIP, clientPort) → (origDstIP, origDstPort)
//  Used by both WinDivert (reverse-NAT) and relay (original dest lookup)
// ════════════════════════════════════════════════════════════════

internal sealed class RedirectNatTable : IDisposable
{
	private readonly TimeSpan _ttl;
	private readonly ConcurrentDictionary<(uint Ip, ushort Port), NatEntry> _map = new();
	private long _lastCleanup = Environment.TickCount64;

	public RedirectNatTable(TimeSpan ttl) => _ttl = ttl;

	public void RecordRedirect(byte[] clientIp, ushort clientPort,
		byte[] origDstIp, ushort origDstPort)
	{
		var key = (ToU32(clientIp), clientPort);
		_map[key] = new NatEntry(
			Clone4(origDstIp), origDstPort,
			Environment.TickCount64 + (long)_ttl.TotalMilliseconds);
		TryCleanup();
	}

	public bool TryGetOrigDest(byte[] clientIp, ushort clientPort,
		out byte[] origDstIp, out ushort origDstPort)
	{
		var key = (ToU32(clientIp), clientPort);
		if (_map.TryGetValue(key, out var entry) && entry.Expire > Environment.TickCount64)
		{
			origDstIp = entry.OrigDstAddr;
			origDstPort = entry.OrigDstPort;
			return true;
		}
		origDstIp = [];
		origDstPort = 0;
		return false;
	}

	public void Dispose() => _map.Clear();

	private void TryCleanup()
	{
		var now = Environment.TickCount64;
		if (now - Interlocked.Read(ref _lastCleanup) < 15_000) return;
		Interlocked.Exchange(ref _lastCleanup, now);
		foreach (var kv in _map)
			if (kv.Value.Expire <= now)
				_map.TryRemove(kv.Key, out _);
	}

	private static uint ToU32(byte[] a) =>
		(uint)(a[0] | (a[1] << 8) | (a[2] << 16) | (a[3] << 24));

	private static byte[] Clone4(byte[] a) { var c = new byte[4]; Buffer.BlockCopy(a, 0, c, 0, 4); return c; }

	private readonly record struct NatEntry(byte[] OrigDstAddr, ushort OrigDstPort, long Expire);
}

// ════════════════════════════════════════════════════════════════
//  Connection info cache (remembers domain per TCP flow for logging)
// ════════════════════════════════════════════════════════════════

internal sealed class ConnectionInfoCache : IDisposable
{
	private readonly TimeSpan _ttl;
	private readonly ConcurrentDictionary<(uint, ushort, uint, ushort), (string Info, long Expire)> _map = new();
	private long _lastCleanup = Environment.TickCount64;

	public ConnectionInfoCache(TimeSpan ttl) => _ttl = ttl;

	public void Record((uint, ushort, uint, ushort) key, string info)
	{
		_map[key] = (info, Environment.TickCount64 + (long)_ttl.TotalMilliseconds);
		TryCleanup();
	}

	public string? TryGet((uint, ushort, uint, ushort) key)
	{
		if (_map.TryGetValue(key, out var entry) && entry.Expire > Environment.TickCount64)
			return entry.Info;
		return null;
	}

	public void Dispose() => _map.Clear();

	private void TryCleanup()
	{
		var now = Environment.TickCount64;
		if (now - Interlocked.Read(ref _lastCleanup) < 15_000) return;
		Interlocked.Exchange(ref _lastCleanup, now);
		foreach (var kv in _map)
			if (kv.Value.Expire <= now)
				_map.TryRemove(kv.Key, out _);
	}
}

// ════════════════════════════════════════════════════════════════
//  TCP table → PID resolver
// ════════════════════════════════════════════════════════════════

internal sealed class TcpOwnerPidResolver : IDisposable
{
	private readonly TimeSpan _cacheTtl;
	private DateTime _lastRefresh = DateTime.MinValue;
	private readonly Dictionary<ConnKey, int> _cache = new();
	private readonly object _lock = new();

	public TcpOwnerPidResolver(TimeSpan cacheTtl) => _cacheTtl = cacheTtl;

	public int? FindOwnerPid(byte[] localAddr, ushort localPort, byte[] remoteAddr, ushort remotePort)
	{
		lock (_lock)
		{
			if (DateTime.UtcNow - _lastRefresh > _cacheTtl)
				Refresh();
			var key = new ConnKey(ToU32(localAddr), localPort, ToU32(remoteAddr), remotePort);
			return _cache.TryGetValue(key, out var pid) ? pid : null;
		}
	}

	public void Dispose() { lock (_lock) _cache.Clear(); }

	private void Refresh()
	{
		int size = 0;
		_ = IpHelperNative.GetExtendedTcpTable(IntPtr.Zero, ref size, true, 2, TcpTableClass.OwnerPidAll, 0);
		var buf = Marshal.AllocHGlobal(size);
		try
		{
			if (IpHelperNative.GetExtendedTcpTable(buf, ref size, true, 2, TcpTableClass.OwnerPidAll, 0) != 0)
				return;
			int count = Marshal.ReadInt32(buf);
			int rowSize = Marshal.SizeOf<MibTcpRowOwnerPid>();
			var ptr = IntPtr.Add(buf, 4);
			_cache.Clear();
			for (int i = 0; i < count; i++)
			{
				var row = Marshal.PtrToStructure<MibTcpRowOwnerPid>(IntPtr.Add(ptr, i * rowSize));
				ushort lp = (ushort)((row.localPort[0] << 8) | row.localPort[1]);
				ushort rp = (ushort)((row.remotePort[0] << 8) | row.remotePort[1]);
				_cache[new ConnKey(row.localAddr, lp, row.remoteAddr, rp)] = (int)row.owningPid;
			}
			_lastRefresh = DateTime.UtcNow;
		}
		finally { Marshal.FreeHGlobal(buf); }
	}

	private static uint ToU32(byte[] a) =>
		(uint)(a[0] | (a[1] << 8) | (a[2] << 16) | (a[3] << 24));

	private readonly record struct ConnKey(uint LocalAddr, ushort LocalPort, uint RemoteAddr, ushort RemotePort);
}

internal enum TcpTableClass { OwnerPidAll = 5 }

[StructLayout(LayoutKind.Sequential)]
internal struct MibTcpRowOwnerPid
{
	public uint state;
	public uint localAddr;
	[MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)] public byte[] localPort;
	public uint remoteAddr;
	[MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)] public byte[] remotePort;
	public uint owningPid;
}

internal static class IpHelperNative
{
	[DllImport("iphlpapi.dll", SetLastError = true)]
	public static extern uint GetExtendedTcpTable(
		IntPtr pTcpTable, ref int dwOutBufLen, bool sort,
		int ipVersion, TcpTableClass tableClass, uint reserved);
}

// ════════════════════════════════════════════════════════════════
//  Process allow-list matcher
// ════════════════════════════════════════════════════════════════

internal sealed class ProcessAllowListMatcher : IDisposable
{
	private readonly HashSet<string> _exactNames;
	private readonly List<Regex> _namePatterns;
	private readonly HashSet<int> _extraPids;
	private readonly HashSet<int> _activePids = new();
	private readonly TimeSpan _refresh;
	private readonly object _lock = new();
	private DateTime _lastRefresh = DateTime.MinValue;

	public ProcessAllowListMatcher(IEnumerable<string> names, IEnumerable<int> pids, TimeSpan refresh)
	{
		_exactNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		_namePatterns = new List<Regex>();
		foreach (var raw in names)
		{
			var normalized = Norm(raw);
			if (normalized.Contains('*') || normalized.Contains('?'))
				_namePatterns.Add(WildcardToRegex(normalized));
			else
				_exactNames.Add(normalized);
		}
		_extraPids = new HashSet<int>(pids);
		_refresh = refresh;
	}

	public bool HasAnyRule => _exactNames.Count > 0 || _namePatterns.Count > 0 || _extraPids.Count > 0;

	public bool ContainsPid(int pid)
	{
		lock (_lock)
		{
			if (_extraPids.Contains(pid)) return true;
			if (DateTime.UtcNow - _lastRefresh > _refresh) RefreshPids();
			return _activePids.Contains(pid);
		}
	}

	public string Describe()
	{
		var allRules = _exactNames.Concat(_namePatterns.Select(p => RegexToWildcardText(p))).OrderBy(x => x).ToArray();
		var n = allRules.Length == 0 ? "none" : string.Join(", ", allRules);
		var p = _extraPids.Count == 0 ? "none" : string.Join(", ", _extraPids.OrderBy(x => x));
		return $"names=[{n}] pids=[{p}]";
	}

	public void Dispose() { lock (_lock) _activePids.Clear(); }

	private void RefreshPids()
	{
		_activePids.Clear();
		foreach (var proc in Process.GetProcesses())
		{
			try
			{
				var exeName = $"{proc.ProcessName}.exe";
				if (IsNameMatched(exeName))
					_activePids.Add(proc.Id);
			}
			catch { }
			finally { proc.Dispose(); }
		}
		_lastRefresh = DateTime.UtcNow;
	}

	private bool IsNameMatched(string exeName)
	{
		if (_exactNames.Contains(exeName)) return true;
		foreach (var pattern in _namePatterns)
			if (pattern.IsMatch(exeName)) return true;
		return false;
	}

	private static Regex WildcardToRegex(string wildcard)
	{
		var pattern = "^" + Regex.Escape(wildcard).Replace("\\*", ".*").Replace("\\?", ".") + "$";
		return new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);
	}

	private static string RegexToWildcardText(Regex regex)
	{
		// Keep Describe() readable for wildcard rules.
		return regex.ToString().Replace("^", "").Replace("$", "").Replace(".*", "*").Replace(".", "?").Replace("\\", "");
	}

	private static string Norm(string s)
	{
		s = s.Trim();
		return s.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ? s : $"{s}.exe";
	}
}

// ════════════════════════════════════════════════════════════════
//  Process name resolver (PID → name cache)
// ════════════════════════════════════════════════════════════════

internal sealed class ProcessNameResolver : IDisposable
{
	private readonly TimeSpan _ttl;
	private readonly Dictionary<int, (string Name, DateTime Expire)> _cache = new();
	private readonly object _lock = new();

	public ProcessNameResolver(TimeSpan ttl) => _ttl = ttl;

	public string Resolve(int pid)
	{
		lock (_lock)
		{
			if (_cache.TryGetValue(pid, out var c) && c.Expire > DateTime.UtcNow)
				return c.Name;
			var name = GetName(pid);
			_cache[pid] = (name, DateTime.UtcNow.Add(_ttl));
			return name;
		}
	}

	public void Dispose() { lock (_lock) _cache.Clear(); }

	private static string GetName(int pid)
	{
		try { using var p = Process.GetProcessById(pid); return $"{p.ProcessName}.exe"; }
		catch { return "unknown.exe"; }
	}
}

// ════════════════════════════════════════════════════════════════
//  Local traffic bypass
// ════════════════════════════════════════════════════════════════

internal sealed class LocalTrafficBypass
{
	private readonly HashSet<uint> _exact;

	private LocalTrafficBypass(HashSet<uint> exact) => _exact = exact;

	public static LocalTrafficBypass CreateDefault()
	{
		var set = new HashSet<uint> { ToU32([127, 0, 0, 1]) };
		AddHost(set, "localhost");
		AddHost(set, Environment.MachineName);
		return new LocalTrafficBypass(set);
	}

	public bool ShouldBypass(byte[] dst)
	{
		if (dst.Length != 4) return false;
		if (dst[0] == 127) return true;
		if (dst[0] == 10) return true;
		if (dst[0] == 172 && dst[1] >= 16 && dst[1] <= 31) return true;
		if (dst[0] == 192 && dst[1] == 168) return true;
		if (dst[0] == 169 && dst[1] == 254) return true;
		return _exact.Contains(ToU32(dst));
	}

	public string DescribeRules() =>
		"127.*/8; 10.*/8; 172.16-31.*/12; 192.168.*/16; 169.254.*/16; localhost; %ComputerName%";

	private static void AddHost(HashSet<uint> set, string host)
	{
		try
		{
			foreach (var a in Dns.GetHostAddresses(host))
				if (a.AddressFamily == AddressFamily.InterNetwork)
					set.Add(ToU32(a.GetAddressBytes()));
		}
		catch { }
	}

	private static uint ToU32(byte[] a) =>
		(uint)(a[0] | (a[1] << 8) | (a[2] << 16) | (a[3] << 24));
}

// ════════════════════════════════════════════════════════════════
//  Runtime statistics
// ════════════════════════════════════════════════════════════════

internal sealed class RedirectStats
{
	private DateTime _lastPrint = DateTime.UtcNow;

	public long ReceivedPackets;
	public long OutboundPackets;
	public long InboundPackets;
	public long NonIpv4Tcp;
	public long LocalBypassed;
	public long PidMiss;
	public long ProcessSkipped;
	public long AlreadyProxy;
	public long Redirected;
	public long InboundRewritten;

	public long ProxiedOk;
	public long ProxiedFail;

	public void TryPrint()
	{
		var now = DateTime.UtcNow;
		if (now - _lastPrint < TimeSpan.FromSeconds(5)) return;
		_lastPrint = now;
		Console.WriteLine(
			$"[stats {DateTime.Now:HH:mm:ss}] " +
			$"recv={ReceivedPackets} out={OutboundPackets} in={InboundPackets} " +
			$"redirected={Redirected} proxied={ProxiedOk} proxyFail={ProxiedFail} " +
			$"inRewrite={InboundRewritten} " +
			$"localBypass={LocalBypassed} pidMiss={PidMiss} procSkip={ProcessSkipped} " +
			$"alreadyProxy={AlreadyProxy} nonTcp={NonIpv4Tcp}");
	}
}

// ════════════════════════════════════════════════════════════════
//  CLI options
// ════════════════════════════════════════════════════════════════

internal sealed record ProxyOptions(
	IReadOnlyCollection<int> ExtraPids,
	IReadOnlyCollection<string> ProcessNames,
	string ProxyHost, ushort ProxyPort, string ProxyScheme)
{
	private static readonly string[] DefaultProcessNames =
	[
		"devenv.exe",
		"blend.exe",
		"servicehub*.exe",
		"microsoft.servicehub*.exe",
		"copilot*.exe",
		"onedrive.exe",
		"perfwatson2.exe",
		"devhub.exe",
		"msbuild*.exe",
		"vstest*.exe",
		"m365copilot.exe",
		"m365copilot_autostarter.exe",
		"m365copilot_widget.exe",
		"webviewhost.exe"
	];

	public static ProxyOptions? Parse(string[] args)
	{
		var pids = new HashSet<int>();
		var names = new HashSet<string>(DefaultProcessNames.Select(Norm), StringComparer.OrdinalIgnoreCase);
		string host = "host.docker.internal";
		ushort port = 7890;
		string scheme = "socks4";

		for (int i = 0; i < args.Length; i++)
		{
			switch (args[i])
			{
				case "--pid":
					if (i + 1 >= args.Length || !int.TryParse(args[++i], out var p)) return null;
					pids.Add(p);
					break;
				case "--process":
					if (i + 1 >= args.Length) return null;
					names.Add(Norm(args[++i]));
					break;
				case "--process-list":
					if (i + 1 >= args.Length) return null;
					names.Clear();
					foreach (var s in args[++i].Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
						names.Add(Norm(s));
					break;
				case "--proxy":
					if (i + 1 >= args.Length) return null;
					var parts = args[++i].Split(':', 2, StringSplitOptions.TrimEntries);
					if (parts.Length != 2 || !ushort.TryParse(parts[1], out var pp)) return null;
					host = parts[0]; port = pp;
					break;
				case "--proxy-scheme":
					if (i + 1 >= args.Length) return null;
					scheme = args[++i].Trim().ToLowerInvariant();
					if (scheme is not ("socks4" or "socks5" or "http")) return null;
					break;
				case "--help" or "-h":
					return null;
			}
		}

		return new ProxyOptions(pids.ToArray(), names.ToArray(), host, port, scheme);
	}

	public static void PrintUsage()
	{
		Console.WriteLine("ForcedProxy — redirect specified processes' traffic through a SOCKS4/SOCKS5/HTTP proxy");
		Console.WriteLine();
		Console.WriteLine("Usage:");
		Console.WriteLine("  ForcedProxy");
		Console.WriteLine("  ForcedProxy --proxy host.docker.internal:7890 --proxy-scheme socks4");
		Console.WriteLine("  ForcedProxy --proxy host.docker.internal:7890 --proxy-scheme socks5");
		Console.WriteLine("  ForcedProxy --proxy 127.0.0.1:7890 --proxy-scheme http");
		Console.WriteLine("  ForcedProxy --process-list \"devenv.exe;msbuild.exe\"");
		Console.WriteLine("  ForcedProxy --pid 1234");
		Console.WriteLine();
		Console.WriteLine("Options:");
		Console.WriteLine("  --proxy HOST:PORT      Upstream proxy address (default: host.docker.internal:7890)");
		Console.WriteLine("  --proxy-scheme SCHEME  socks4 | socks5 | http (default: socks4)");
		Console.WriteLine("  --process NAME         Add a process name to intercept");
		Console.WriteLine("  --process-list LIST    Replace default process list (semicolon-separated)");
		Console.WriteLine("  --pid PID              Add a specific PID to intercept");
	}

	private static string Norm(string s)
	{
		s = s.Trim();
		return s.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ? s : $"{s}.exe";
	}
}

// ════════════════════════════════════════════════════════════════
//  Skip log dedup (avoid flooding console with repeated skip messages)
// ════════════════════════════════════════════════════════════════

internal sealed class SkipLogDedup
{
	private readonly TimeSpan _window;
	private readonly ConcurrentDictionary<(string Ip, ushort Port), long> _seen = new();

	public SkipLogDedup(TimeSpan window) => _window = window;

	/// <summary>Returns true if this (ip, port) hasn't been logged within the dedup window.</summary>
	public bool ShouldLog(string ip, ushort port)
	{
		var now = Environment.TickCount64;
		var key = (ip, port);
		if (_seen.TryGetValue(key, out var lastTick) && now - lastTick < (long)_window.TotalMilliseconds)
			return false;
		_seen[key] = now;
		return true;
	}
}
