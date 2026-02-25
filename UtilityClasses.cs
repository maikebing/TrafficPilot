using System.Diagnostics;
using System.Text.RegularExpressions;

namespace TrafficPilot;

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
			foreach (var a in System.Net.Dns.GetHostAddresses(host))
				if (a.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
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
}

// ════════════════════════════════════════════════════════════════
//  Skip log dedup
// ════════════════════════════════════════════════════════════════

internal sealed class SkipLogDedup
{
	private readonly TimeSpan _window;
	private readonly System.Collections.Concurrent.ConcurrentDictionary<(string Ip, ushort Port), long> _seen = new();

	public SkipLogDedup(TimeSpan window) => _window = window;

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

	private static string Norm(string s)
	{
		s = s.Trim();
		return s.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ? s : $"{s}.exe";
	}
}
