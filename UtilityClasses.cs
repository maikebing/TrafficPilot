using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text.RegularExpressions;

namespace TrafficPilot;

// ════════════════════════════════════════════════════════════════
//  Rule normalizers
// ════════════════════════════════════════════════════════════════

internal static class TargetRuleNormalizer
{
	public static string NormalizeProcessName(string s)
	{
		if (string.IsNullOrWhiteSpace(s))
			return string.Empty;

		s = s.Trim().ToLowerInvariant();
		return s.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ? s : $"{s}.exe";
	}

	public static string NormalizeDomain(string s)
	{
		if (string.IsNullOrWhiteSpace(s))
			return string.Empty;

		s = s.Trim();
		if (s.Contains("://", StringComparison.Ordinal) && Uri.TryCreate(s, UriKind.Absolute, out var uri) && !string.IsNullOrWhiteSpace(uri.Host))
			s = uri.Host;
		else
		{
			int slashIndex = s.IndexOf('/');
			if (slashIndex >= 0)
				s = s[..slashIndex];

			if (!s.Contains('*') && !s.Contains('?'))
			{
				int colonIndex = s.LastIndexOf(':');
				if (colonIndex > 0 && s.IndexOf(':') == colonIndex)
					s = s[..colonIndex];
			}
		}

		return s.Trim().TrimEnd('.').ToLowerInvariant();
	}
}

// ════════════════════════════════════════════════════════════════
//  Process allow-list matcher
// ════════════════════════════════════════════════════════════════

internal sealed class ProcessAllowListMatcher : IDisposable
{
	private readonly HashSet<string> _exactNames = new(StringComparer.OrdinalIgnoreCase);
	private readonly List<Regex> _namePatterns = [];
	private readonly TimeSpan _refresh;
	private readonly object _lock = new();
	private readonly HashSet<int> _activePids = [];
	private DateTime _lastRefresh = DateTime.MinValue;

	public ProcessAllowListMatcher(IEnumerable<string> names, TimeSpan refresh)
	{
		ArgumentNullException.ThrowIfNull(names);

		foreach (var raw in names)
		{
			var normalized = TargetRuleNormalizer.NormalizeProcessName(raw);
			if (string.IsNullOrWhiteSpace(normalized))
				continue;

			if (normalized.Contains('*') || normalized.Contains('?'))
				_namePatterns.Add(WildcardToRegex(normalized));
			else
				_exactNames.Add(normalized);
		}

		_refresh = refresh;
	}

	public bool HasAnyRule => _exactNames.Count > 0 || _namePatterns.Count > 0;

	public bool ContainsPid(int pid)
	{
		lock (_lock)
		{
			if (DateTime.UtcNow - _lastRefresh > _refresh)
				RefreshPids();

			return _activePids.Contains(pid);
		}
	}

	public string Describe()
	{
		var allRules = _exactNames.Concat(_namePatterns.Select(RegexToWildcardText)).OrderBy(x => x).ToArray();
		return allRules.Length == 0 ? "none" : string.Join(", ", allRules);
	}

	public void Dispose()
	{
		lock (_lock)
			_activePids.Clear();
	}

	private void RefreshPids()
	{
		_activePids.Clear();
		foreach (var proc in Process.GetProcesses())
		{
			try
			{
				var exeName = TargetRuleNormalizer.NormalizeProcessName(proc.ProcessName);
				if (IsNameMatched(exeName))
					_activePids.Add(proc.Id);
			}
			catch
			{
			}
			finally
			{
				proc.Dispose();
			}
		}

		_lastRefresh = DateTime.UtcNow;
	}

	private bool IsNameMatched(string exeName)
	{
		if (_exactNames.Contains(exeName))
			return true;

		foreach (var pattern in _namePatterns)
			if (pattern.IsMatch(exeName))
				return true;

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
}

// ════════════════════════════════════════════════════════════════
//  Domain rule matcher
// ════════════════════════════════════════════════════════════════

internal sealed class DomainRuleMatcher
{
	private readonly HashSet<string> _exactDomains = new(StringComparer.OrdinalIgnoreCase);
	private readonly List<Regex> _domainPatterns = [];

	public DomainRuleMatcher(IEnumerable<string> rules)
	{
		ArgumentNullException.ThrowIfNull(rules);

		foreach (var raw in rules)
		{
			var normalized = TargetRuleNormalizer.NormalizeDomain(raw);
			if (string.IsNullOrWhiteSpace(normalized))
				continue;

			if (normalized.Contains('*') || normalized.Contains('?'))
				_domainPatterns.Add(WildcardToRegex(normalized));
			else
				_exactDomains.Add(normalized);
		}
	}

	public bool HasAnyRule => _exactDomains.Count > 0 || _domainPatterns.Count > 0;

	public bool IsMatch(string? domain)
	{
		if (string.IsNullOrWhiteSpace(domain))
			return false;

		var normalized = TargetRuleNormalizer.NormalizeDomain(domain);
		if (normalized.Length == 0)
			return false;

		if (_exactDomains.Contains(normalized))
			return true;

		foreach (var pattern in _domainPatterns)
			if (pattern.IsMatch(normalized))
				return true;

		return false;
	}

	public string Describe()
	{
		var allRules = _exactDomains.Concat(_domainPatterns.Select(RegexToWildcardText)).OrderBy(x => x).ToArray();
		return allRules.Length == 0 ? "none" : string.Join(", ", allRules);
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
//  Windows startup registration via registry
// ════════════════════════════════════════════════════════════════

internal static class StartupManager
{
	private const string RunKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
	private const string AppName = "TrafficPilot";

	/// <summary>Returns true if a startup entry for this app exists in the current user's registry.</summary>
	public static bool IsEnabled()
	{
		using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(RunKey, writable: false);
		return key?.GetValue(AppName) is not null;
	}

	/// <summary>Creates or updates the startup registry entry pointing to the current executable.</summary>
	public static void Enable()
	{
		var exePath = Environment.ProcessPath
			?? Path.Combine(AppContext.BaseDirectory, $"{AppName}.exe");
		using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(RunKey, writable: true)
			?? throw new InvalidOperationException("Cannot open registry startup key.");
		key.SetValue(AppName, $"\"{exePath}\"");
	}

	/// <summary>Removes the startup registry entry if it exists.</summary>
	public static void Disable()
	{
		using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(RunKey, writable: true);
		key?.DeleteValue(AppName, throwOnMissingValue: false);
	}
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
	IReadOnlyCollection<string> ProcessNames,
	IReadOnlyCollection<string> DomainRules,
	string ProxyHost, ushort ProxyPort, string ProxyScheme,
	bool ProxyEnabled = true,
	bool HostsRedirectEnabled = false,
	string HostsRedirectUrl = GitHub520HostsProvider.DefaultUrl)
{
    internal static readonly string[] DefaultProcessNames =
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

	internal static readonly string[] DefaultDomainRules =
	[
		"github.com",
		"*.github.com",
		"github.io",
		"github.blog",
		"githubstatus.com",
		"github.community",
		"*.githubusercontent.com",
		"*.githubassets.com",
		"*.githubcopilot.com",
		"*.s3.amazonaws.com",
		"*.fastly.net",
		"vscode.dev"
	];

	internal static readonly string[] DefaultRefreshDomains =
	[
		"alive.github.com",
		"api.github.com",
		"api.individual.githubcopilot.com",
		"avatars.githubusercontent.com",
		"avatars0.githubusercontent.com",
		"avatars1.githubusercontent.com",
		"avatars2.githubusercontent.com",
		"avatars3.githubusercontent.com",
		"avatars4.githubusercontent.com",
		"avatars5.githubusercontent.com",
		"camo.githubusercontent.com",
		"central.github.com",
		"cloud.githubusercontent.com",
		"codeload.github.com",
		"collector.github.com",
		"desktop.githubusercontent.com",
		"favicons.githubusercontent.com",
		"gist.github.com",
		"github-cloud.s3.amazonaws.com",
		"github-com.s3.amazonaws.com",
		"github-production-release-asset-2e65be.s3.amazonaws.com",
		"github-production-repository-file-5c1aeb.s3.amazonaws.com",
		"github-production-user-asset-6210df.s3.amazonaws.com",
		"github.blog",
		"github.com",
		"github.community",
		"github.githubassets.com",
		"github.global.ssl.fastly.net",
		"github.io",
		"github.map.fastly.net",
		"githubstatus.com",
		"live.github.com",
		"media.githubusercontent.com",
		"objects.githubusercontent.com",
		"pipelines.actions.githubusercontent.com",
		"raw.githubusercontent.com",
		"user-images.githubusercontent.com",
		"vscode.dev",
		"education.github.com",
		"private-user-images.githubusercontent.com",
	];

	public static ProxyOptions? Parse(string[] args)
	{
		var processNames = new HashSet<string>(DefaultProcessNames.Select(TargetRuleNormalizer.NormalizeProcessName), StringComparer.OrdinalIgnoreCase);
		var domains = new HashSet<string>(DefaultDomainRules.Select(TargetRuleNormalizer.NormalizeDomain), StringComparer.OrdinalIgnoreCase);
		string host = "host.docker.internal";
		ushort port = 7890;
		string scheme = "socks4";

		for (int i = 0; i < args.Length; i++)
		{
			switch (args[i])
			{
				case "--process":
					if (i + 1 >= args.Length) return null;
					processNames.Add(TargetRuleNormalizer.NormalizeProcessName(args[++i]));
					break;
				case "--process-list":
					if (i + 1 >= args.Length) return null;
					processNames.Clear();
					foreach (var s in args[++i].Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
						processNames.Add(TargetRuleNormalizer.NormalizeProcessName(s));
					break;
				case "--domain":
					if (i + 1 >= args.Length) return null;
					domains.Add(TargetRuleNormalizer.NormalizeDomain(args[++i]));
					break;
				case "--domain-list":
					if (i + 1 >= args.Length) return null;
					domains.Clear();
					foreach (var s in args[++i].Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
						domains.Add(TargetRuleNormalizer.NormalizeDomain(s));
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

		return new ProxyOptions(
			processNames.Where(static x => x.Length > 0).ToArray(),
			domains.Where(static x => x.Length > 0).ToArray(),
			host,
			port,
			scheme);
	}
}

// ════════════════════════════════════════════════════════════════
//  Local network helpers
// ════════════════════════════════════════════════════════════════

internal static class LocalNetworkHelper
{
	/// <summary>
	/// Returns IPv4 addresses of network interfaces that are Up and have at least
	/// one usable default gateway configured.
	/// </summary>
	public static IReadOnlyList<string> GetLocalIpsWithGateway()
	{
		var result = new List<string>();

		foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
		{
			if (nic.OperationalStatus != OperationalStatus.Up)
				continue;

			if (nic.NetworkInterfaceType is NetworkInterfaceType.Loopback
				or NetworkInterfaceType.Tunnel)
				continue;

			var props = nic.GetIPProperties();

			var hasGateway = props.GatewayAddresses
				.Any(g => g.Address.AddressFamily == AddressFamily.InterNetwork
					&& !g.Address.Equals(System.Net.IPAddress.Any));

			if (!hasGateway)
				continue;

			foreach (var uni in props.UnicastAddresses)
			{
				if (uni.Address.AddressFamily != AddressFamily.InterNetwork)
					continue;

				result.Add(uni.Address.ToString());
			}
		}

		return result;
	}
}
