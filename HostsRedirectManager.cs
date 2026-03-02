using System.ComponentModel;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;

namespace TrafficPilot;

// ════════════════════════════════════════════════════════════════
//  GitHub520 Hosts Provider
// ════════════════════════════════════════════════════════════════

internal sealed class GitHub520HostsProvider : IDisposable
{
	public const string DefaultUrl =
		"https://raw.githubusercontent.com/521xueweihan/GitHub520/refs/heads/main/hosts";

	private readonly string _url;
	private readonly HttpClient _http;
	private Dictionary<string, byte[]> _hostsMap = new(StringComparer.OrdinalIgnoreCase);
	private readonly object _lock = new();
	private DateTime _lastRefresh = DateTime.MinValue;

	public int HostCount { get { lock (_lock) return _hostsMap.Count; } }
	public DateTime LastRefresh => _lastRefresh;

	public event Action<string>? OnLog;

	public GitHub520HostsProvider(string? url = null)
	{
		_url = url ?? DefaultUrl;
		_http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
	}

	public async Task RefreshAsync(CancellationToken ct = default)
	{
		try
		{
			var content = await _http.GetStringAsync(_url, ct);
			var map = ParseHosts(content);
			lock (_lock)
			{
				_hostsMap = map;
				_lastRefresh = DateTime.UtcNow;
			}
			OnLog?.Invoke($"[hosts] Loaded {map.Count} entries from GitHub520");
		}
		catch (Exception ex)
		{
			OnLog?.Invoke($"[hosts] Failed to refresh: {ex.Message}");
		}
	}

	public bool TryGetRedirectIp(string domain, out byte[] ip)
	{
		lock (_lock)
		{
			if (_hostsMap.TryGetValue(domain, out var found))
			{
				ip = found;
				return true;
			}
			ip = [];
			return false;
		}
	}

	public void Dispose() => _http.Dispose();

	private static Dictionary<string, byte[]> ParseHosts(string content)
	{
		var map = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);
		foreach (var rawLine in content.Split('\n', StringSplitOptions.RemoveEmptyEntries))
		{
			var line = rawLine.Trim();
			if (line.StartsWith('#') || string.IsNullOrEmpty(line)) continue;
			int commentIdx = line.IndexOf('#');
			if (commentIdx >= 0) line = line[..commentIdx].Trim();

			var parts = line.Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
			if (parts.Length < 2) continue;

			if (!IPAddress.TryParse(parts[0], out var addr) || addr.AddressFamily != AddressFamily.InterNetwork)
				continue;

			var ipBytes = addr.GetAddressBytes();
			for (int i = 1; i < parts.Length; i++)
				map[parts[i]] = ipBytes;
		}
		return map;
	}
}

// ════════════════════════════════════════════════════════════════
//  DNS Interceptor
// ════════════════════════════════════════════════════════════════

internal sealed class DnsInterceptor : IDisposable
{
	private readonly GitHub520HostsProvider _hostsProvider;
	private IntPtr _winDivertHandle = IntPtr.Zero;
	private CancellationTokenSource? _cts;
	private Task? _loopTask;

	public event Action<string>? OnLog;

	public DnsInterceptor(GitHub520HostsProvider hostsProvider)
	{
		_hostsProvider = hostsProvider;
	}

	public async Task StartAsync()
	{
		// Capture inbound DNS responses (src port 53 = DNS server replies to us)
		_winDivertHandle = WinDivertNative.WinDivertOpen(
			"udp and udp.SrcPort == 53",
			WinDivertLayer.Network, 0, 0UL);
		if (_winDivertHandle == IntPtr.Zero || _winDivertHandle == new IntPtr(-1))
		{
			var err = Marshal.GetLastWin32Error();
			throw new Win32Exception(err, $"WinDivertOpen for DNS failed (err={err})");
		}
		_cts = new CancellationTokenSource();
		_loopTask = DnsLoopAsync(_cts.Token);
		await Task.Delay(50);
	}

	public async Task StopAsync()
	{
		_cts?.Cancel();
		if (_loopTask != null)
		{
			try { await _loopTask; }
			catch (OperationCanceledException) { }
		}
	}

	public void Dispose()
	{
		_cts?.Dispose();
		_loopTask?.Dispose();
		if (_winDivertHandle != IntPtr.Zero && _winDivertHandle != new IntPtr(-1))
			WinDivertNative.WinDivertClose(_winDivertHandle);
	}

	private async Task DnsLoopAsync(CancellationToken ct)
	{
		var packet = new byte[0xFFFF];
		while (!ct.IsCancellationRequested)
		{
			try
			{
				var addr = new WinDivertAddress();
				if (!WinDivertNative.WinDivertRecv(_winDivertHandle, packet, (uint)packet.Length, out var recvLen, ref addr))
					continue;

				if (TryPatchDnsResponse(packet, (int)recvLen))
					WinDivertNative.WinDivertHelperCalcChecksums(packet, recvLen, ref addr, 0UL);

				WinDivertNative.WinDivertSend(_winDivertHandle, packet, recvLen, out _, ref addr);
			}
			catch (OperationCanceledException) { break; }
			catch (Exception ex) { OnLog?.Invoke($"[dns] Error: {ex.Message}"); }

			await Task.Yield();
		}
	}

	private bool TryPatchDnsResponse(byte[] buf, int len)
	{
		if (len < 28) return false;
		int ihl = (buf[0] & 0xF) * 4;
		if (ihl < 20 || buf[9] != 17) return false; // must be UDP

		int dnsStart = ihl + 8;
		int dnsLen = len - dnsStart;
		if (dnsLen < 12) return false;

		// QR bit must be set (response)
		if ((buf[dnsStart + 2] & 0x80) == 0) return false;

		int qdCount = (buf[dnsStart + 4] << 8) | buf[dnsStart + 5];
		int anCount = (buf[dnsStart + 6] << 8) | buf[dnsStart + 7];
		if (anCount == 0 || qdCount == 0) return false;

		// Parse first question domain
		int pos = dnsStart + 12;
		var domain = ParseDnsName(buf, dnsStart, dnsLen, ref pos);
		if (domain is null) return false;

		// Skip QTYPE + QCLASS (4 bytes per question)
		if (pos + 4 > dnsStart + dnsLen) return false;
		pos += 4;

		for (int q = 1; q < qdCount; q++)
		{
			if (!SkipDnsName(buf, dnsStart, dnsLen, ref pos)) return false;
			if (pos + 4 > dnsStart + dnsLen) return false;
			pos += 4;
		}

		if (!_hostsProvider.TryGetRedirectIp(domain, out var newIp))
			return false;

		// Patch A records in answer section
		bool patched = false;
		for (int i = 0; i < anCount; i++)
		{
			if (pos >= dnsStart + dnsLen) break;
			if (!SkipDnsName(buf, dnsStart, dnsLen, ref pos)) break;
			if (pos + 10 > dnsStart + dnsLen) break;

			int rType = (buf[pos] << 8) | buf[pos + 1];
			int rdLen = (buf[pos + 8] << 8) | buf[pos + 9];
			pos += 10;

			if (rType == 1 && rdLen == 4 && pos + 4 <= dnsStart + dnsLen)
			{
				Buffer.BlockCopy(newIp, 0, buf, pos, 4);
				OnLog?.Invoke($"[dns] {domain} -> {newIp[0]}.{newIp[1]}.{newIp[2]}.{newIp[3]}");
				patched = true;
			}

			if (pos + rdLen > dnsStart + dnsLen) break;
			pos += rdLen;
		}
		return patched;
	}

	private static string? ParseDnsName(byte[] buf, int dnsStart, int dnsLen, ref int pos)
	{
		var sb = new StringBuilder();
		int absoluteLimit = dnsStart + dnsLen;
		bool first = true;

		while (pos < absoluteLimit)
		{
			byte lenByte = buf[pos];
			if (lenByte == 0) { pos++; return sb.Length > 0 ? sb.ToString() : null; }
			if ((lenByte & 0xC0) == 0xC0)
			{
				if (pos + 1 >= absoluteLimit) return null;
				int ptrOffset = ((lenByte & 0x3F) << 8) | buf[pos + 1];
				pos += 2;
				var ptrPos = dnsStart + ptrOffset;
				while (ptrPos < absoluteLimit)
				{
					byte pLen = buf[ptrPos];
					if (pLen == 0 || (pLen & 0xC0) == 0xC0) break;
					if (!first) sb.Append('.');
					else first = false;
					if (ptrPos + 1 + pLen > absoluteLimit) break;
					sb.Append(Encoding.ASCII.GetString(buf, ptrPos + 1, pLen));
					ptrPos += 1 + pLen;
				}
				return sb.Length > 0 ? sb.ToString() : null;
			}
			if (!first) sb.Append('.');
			else first = false;
			if (pos + 1 + lenByte > absoluteLimit) return null;
			sb.Append(Encoding.ASCII.GetString(buf, pos + 1, lenByte));
			pos += 1 + lenByte;
		}
		return null;
	}

	private static bool SkipDnsName(byte[] buf, int dnsStart, int dnsLen, ref int pos)
	{
		int absoluteLimit = dnsStart + dnsLen;
		while (pos < absoluteLimit)
		{
			byte len = buf[pos];
			if (len == 0) { pos++; return true; }
			if ((len & 0xC0) == 0xC0) { pos += 2; return true; }
			pos += 1 + len;
		}
		return false;
	}
}
