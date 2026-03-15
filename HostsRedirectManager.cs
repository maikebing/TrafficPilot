using System.ComponentModel;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;

namespace TrafficPilot;

// ════════════════════════════════════════════════════════════════
//  GitHub520 Hosts Provider
// ════════════════════════════════════════════════════════════════

internal sealed class GitHub520HostsProvider : IDisposable
{
	public const string DefaultUrl =
        "https://raw.hellogithub.com/hosts";

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

	/// <summary>Merges resolved IPs into the live hosts map without a full reload.</summary>
	public void BatchUpdate(IReadOnlyDictionary<string, byte[]> updates)
	{
		lock (_lock)
			foreach (var (domain, ip) in updates)
				_hostsMap[domain] = ip;
	}

	/// <summary>Returns a snapshot of the current hosts map for external use.</summary>
	public Dictionary<string, byte[]> GetHostsMap()
	{
		lock (_lock)
			return new Dictionary<string, byte[]>(_hostsMap, StringComparer.OrdinalIgnoreCase);
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

// ════════════════════════════════════════════════════════════════
//  Domain IP Fetch Result
// ════════════════════════════════════════════════════════════════

internal sealed record DomainIpResult(
	string Domain,
	string? Ip,
	long LatencyMs,
	string? DohSource,
	string? Error = null,
	long ProxyLatencyMs = -1,
	string? ProxyError = null);

// ════════════════════════════════════════════════════════════════
//  IP Fetch Service  (GitHub520-style: multi-DoH + TCP latency)
// ════════════════════════════════════════════════════════════════

internal sealed class IpFetchService : IDisposable
{
	private static readonly (string Name, string UrlTemplate)[] DoHProviders =
	[
		// ── Domain-based DoH (may fail if domain DNS itself is hijacked) ──────
		("Google",          "https://dns.google/resolve?name={0}&type=A"),
		("Cloudflare",      "https://cloudflare-dns.com/dns-query?name={0}&type=A"),
		// AliDNS may return incorrect IPs for GitHub domains in some regions
		("AliDNS",          "https://dns.alidns.com/resolve?name={0}&type=A"),
		// DNSPod (Tencent Cloud) �?good coverage in mainland China
		("DNSPod",          "https://doh.pub/dns-query?name={0}&type=A"),

		// ── IP-based DoH �?bypass DNS hijacking entirely ──────────────────────
		("Google-IP",       "https://8.8.8.8/resolve?name={0}&type=A"),
		("Cloudflare-IP",   "https://1.1.1.1/dns-query?name={0}&type=A"),
		("Quad101",         "https://101.101.101.101/dns-query?name={0}&type=A"),
		("Quad101-Alt",     "https://101.102.103.104/dns-query?name={0}&type=A"),
		// DNSPod IP (Tencent)
		("DNSPod-IP",       "https://119.29.29.29/dns-query?name={0}&type=A"),
		("DNSPod-IP2",      "https://120.53.53.53/dns-query?name={0}&type=A"),
		// 114DNS �?traditional Chinese public DNS, DoH support may be limited
		("114DNS",          "https://114.114.114.114/dns-query?name={0}&type=A"),
		("114DNS-Alt",      "https://114.114.115.115/dns-query?name={0}&type=A"),
	];

	private readonly HttpClient _http;
	private readonly int _tcpTimeoutMs;
	private readonly string? _proxyHost;
	private readonly int _proxyPort;
	private readonly string _proxyScheme;
	private const int LatencyRounds = 2;  // TCP tests per candidate; best (min) result is kept

	public IpFetchService(int tcpTimeoutMs = 1500, string? proxyHost = null, int proxyPort = 0, string proxyScheme = "socks4")
	{
		_http = new HttpClient { Timeout = TimeSpan.FromSeconds(8) };
		_http.DefaultRequestHeaders.Add("Accept", "application/dns-json");
		_tcpTimeoutMs = tcpTimeoutMs;
		_proxyHost = string.IsNullOrWhiteSpace(proxyHost) ? null : proxyHost;
		_proxyPort = proxyPort;
		_proxyScheme = proxyScheme;
	}

	/// <summary>
	/// Resolves the best IP (lowest TCP latency) for each concrete domain,
	/// yielding results as they complete via multiple DoH providers.
	/// Wildcard rules (*.github.com) are skipped automatically.
	/// </summary>
	public async IAsyncEnumerable<DomainIpResult> FetchAllAsync(
		IEnumerable<string> domains,
		[EnumeratorCancellation] CancellationToken ct = default)
	{
		var concreteDomains = domains
			.Where(static d => !string.IsNullOrWhiteSpace(d)
							&& !d.Contains('*')
							&& !d.Contains('?'))
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.ToArray();

		var tasks = concreteDomains.Select(d => FetchBestIpAsync(d, ct));

		await foreach (var task in Task.WhenEach(tasks).WithCancellation(ct).ConfigureAwait(false))
		{
			DomainIpResult result;
			try
			{
				result = await task.ConfigureAwait(false);
			}
			catch (OperationCanceledException)
			{
				yield break;
			}
			catch (Exception ex)
			{
				result = new DomainIpResult(string.Empty, null, -1, null, ex.Message);
			}

			yield return result;
		}
	}

	public void Dispose() => _http.Dispose();

	private async Task<DomainIpResult> FetchBestIpAsync(string domain, CancellationToken ct)
	{
		try
		{
			// Round 1: query all providers concurrently
			var candidates = await ResolveAsync(domain, DoHProviders, ct);

			// Round 2: if nothing resolved, retry with IP-based providers only.
			// IP-based DoH bypasses local DNS hijacking and is the most reliable fallback.
			if (candidates.Length == 0)
			{
				await Task.Delay(500, ct).ConfigureAwait(false);
				var ipProviders = DoHProviders
					.Where(static p => IsIpBasedUrl(p.UrlTemplate))
					.ToArray();
				candidates = await ResolveAsync(domain, ipProviders, ct);
			}

			if (candidates.Length == 0)
				return new DomainIpResult(domain, null, -1, null, "No IPs resolved");

			// Test TCP latency across LatencyRounds rounds per candidate; return minimum
			var latencyTasks = candidates
				.Select(c => TestBestLatencyAsync(domain, c.Ip, c.Source, ct))
				.ToArray();

			var latencyResults = await Task.WhenAll(latencyTasks).ConfigureAwait(false);

					var best = latencyResults
						.Where(static r => r.LatencyMs >= 0)
						.OrderBy(static r => r.LatencyMs)
						.FirstOrDefault() ?? latencyResults[0];

					if (best.Ip is not null && _proxyHost is not null && _proxyPort > 0)
					{
						var (proxyMs, proxyErr) = await TestProxyLatencyAsync(best.Ip, ct).ConfigureAwait(false);
						return best with { ProxyLatencyMs = proxyMs, ProxyError = proxyErr };
					}

					return best;
				}
				catch (OperationCanceledException)
				{
					throw;
				}
				catch (Exception ex)
				{
					return new DomainIpResult(domain, null, -1, null, ex.Message);
				}
			}

	/// <summary>Queries a set of DoH providers concurrently and returns distinct resolved IPs.</summary>
	private async Task<(string Ip, string Source)[]> ResolveAsync(
		string domain,
		IEnumerable<(string Name, string UrlTemplate)> providers,
		CancellationToken ct)
	{
		var tasks = providers
			.Select(p => QueryDoHAsync(p.Name, string.Format(p.UrlTemplate, domain), ct))
			.ToArray();

		var results = await Task.WhenAll(tasks).ConfigureAwait(false);

		return results
			.Where(static x => x.Ip is not null)
			.DistinctBy(static x => x.Ip, StringComparer.OrdinalIgnoreCase)
			.Select(static x => (x.Ip!, x.Source))
			.ToArray();
	}

	/// <summary>
	/// Runs <see cref="LatencyRounds"/> TCP tests against a candidate IP
	/// and returns the result with the lowest observed latency.
	/// </summary>
	private async Task<DomainIpResult> TestBestLatencyAsync(
		string domain, string ip, string source, CancellationToken ct)
	{
		long bestMs = long.MaxValue;
		string? lastError = null;

		for (int i = 0; i < LatencyRounds; i++)
		{
			var r = await TestLatencyAsync(domain, ip, source, ct).ConfigureAwait(false);
			if (r.LatencyMs >= 0)
				bestMs = Math.Min(bestMs, r.LatencyMs);
			else
				lastError ??= r.Error;
		}

		return bestMs != long.MaxValue
			? new DomainIpResult(domain, ip, bestMs, source)
			: new DomainIpResult(domain, ip, -1, source, lastError);
	}

	/// <summary>Returns true when the DoH URL template uses a bare IP address as host (bypasses DNS hijacking).</summary>
	private static bool IsIpBasedUrl(string urlTemplate)
	{
		try
		{
			var host = new Uri(string.Format(urlTemplate, "test")).Host;
			return IPAddress.TryParse(host, out _);
		}
		catch
		{
			return false;
		}
	}

	private async Task<(string? Ip, string Source)> QueryDoHAsync(
		string providerName, string url, CancellationToken ct)
	{
		try
		{
			using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
			cts.CancelAfter(TimeSpan.FromSeconds(6));

			var json = await _http.GetStringAsync(url, cts.Token).ConfigureAwait(false);
			using var doc = JsonDocument.Parse(json);

			if (!doc.RootElement.TryGetProperty("Answer", out var answers))
				return (null, providerName);

			foreach (var answer in answers.EnumerateArray())
			{
				if (answer.TryGetProperty("type", out var typeEl) && typeEl.GetInt32() == 1 &&
					answer.TryGetProperty("data", out var dataEl))
				{
					var ip = dataEl.GetString();
					if (!string.IsNullOrWhiteSpace(ip) && IPAddress.TryParse(ip, out _))
						return (ip, providerName);
				}
			}

			return (null, providerName);
		}
		catch
		{
			return (null, providerName);
		}
	}

	private async Task<(long LatencyMs, string? Error)> TestProxyLatencyAsync(string targetIp, CancellationToken ct)
	{
		try
		{
			using var tcp = new TcpClient();
			using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
			cts.CancelAfter(_tcpTimeoutMs * 2);

			var sw = Stopwatch.StartNew();
			await tcp.ConnectAsync(_proxyHost!, _proxyPort, cts.Token).ConfigureAwait(false);
			var stream = tcp.GetStream();

			bool ok = _proxyScheme.ToLowerInvariant() switch
			{
				"socks5"          => await Socks5ConnectAsync(stream, targetIp, 443, cts.Token).ConfigureAwait(false),
				"http" or "https" => await HttpConnectAsync(stream, targetIp, 443, cts.Token).ConfigureAwait(false),
				_                 => await Socks4ConnectAsync(stream, targetIp, 443, cts.Token).ConfigureAwait(false)
			};

			sw.Stop();

			return ok ? (sw.ElapsedMilliseconds, null) : (-1L, "Proxy CONNECT failed");
		}
		catch (OperationCanceledException) when (!ct.IsCancellationRequested)
		{
			return (-1, "Proxy timeout");
		}
		catch (OperationCanceledException)
		{
			throw;
		}
		catch (Exception ex)
		{
			return (-1, ex.Message);
		}
	}

	private static async Task<bool> Socks4ConnectAsync(NetworkStream s, string ip, int port, CancellationToken ct)
	{
		if (!IPAddress.TryParse(ip, out var addr)) return false;
		var b = addr.GetAddressBytes();
		byte[] req = [4, 1, (byte)(port >> 8), (byte)(port & 0xFF), b[0], b[1], b[2], b[3], 0];
		await s.WriteAsync(req, ct).ConfigureAwait(false);
		var resp = new byte[8];
		await s.ReadAsync(resp.AsMemory(0, 8), ct).ConfigureAwait(false);

		return resp[1] == 90;
	}

	private static async Task<bool> Socks5ConnectAsync(NetworkStream s, string ip, int port, CancellationToken ct)
	{
		if (!IPAddress.TryParse(ip, out var addr)) return false;
		await s.WriteAsync((byte[])[5, 1, 0], ct).ConfigureAwait(false);
		var auth = new byte[2];
		await s.ReadAsync(auth.AsMemory(0, 2), ct).ConfigureAwait(false);
		if (auth[0] != 5 || auth[1] != 0) return false;

		var b = addr.GetAddressBytes();
		byte[] req = [5, 1, 0, 1, b[0], b[1], b[2], b[3], (byte)(port >> 8), (byte)(port & 0xFF)];
		await s.WriteAsync(req, ct).ConfigureAwait(false);
		var resp = new byte[10];
		await s.ReadAsync(resp.AsMemory(0, 10), ct).ConfigureAwait(false);

		return resp[0] == 5 && resp[1] == 0;
	}

	private static async Task<bool> HttpConnectAsync(NetworkStream s, string ip, int port, CancellationToken ct)
	{
		var req = Encoding.ASCII.GetBytes($"CONNECT {ip}:{port} HTTP/1.1\r\nHost: {ip}:{port}\r\n\r\n");
		await s.WriteAsync(req, ct).ConfigureAwait(false);
		var buf = new byte[256];
		int n = await s.ReadAsync(buf.AsMemory(0, 256), ct).ConfigureAwait(false);

		return Encoding.ASCII.GetString(buf, 0, n).Contains("200");
	}

	private async Task<DomainIpResult> TestLatencyAsync(
		string domain, string ip, string source, CancellationToken ct)
	{
		try
		{
			using var tcp = new TcpClient();
			using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
			cts.CancelAfter(_tcpTimeoutMs);

			var sw = Stopwatch.StartNew();
			await tcp.ConnectAsync(ip, 443, cts.Token).ConfigureAwait(false);
			sw.Stop();

			return new DomainIpResult(domain, ip, sw.ElapsedMilliseconds, source);
		}
		catch (OperationCanceledException) when (!ct.IsCancellationRequested)
		{
			return new DomainIpResult(domain, ip, -1, source, "Timeout");
		}
		catch (OperationCanceledException)
		{
			throw;
		}
		catch (Exception ex)
		{
			return new DomainIpResult(domain, ip, -1, source, ex.Message);
		}
	}
}
