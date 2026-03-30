using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
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

		s = s.Trim();
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
		key.SetValue(AppName, $"\"{exePath}\" --minimized");
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
	public long Bytes;

	public static string FormatBytes(long bytes)
	{
		const long KB = 1024L;
		const long MB = 1024L * 1024;
		const long GB = 1024L * 1024 * 1024;

		if (bytes >= GB)
			return $"{bytes / (double)GB:F2} GB";
		if (bytes >= MB)
			return $"{bytes / (double)MB:F2} MB";
		if (bytes >= KB)
			return $"{bytes / (double)KB:F2} KB";
		return $"{bytes} B";
	}
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
	string HostsRedirectUrl = GitHub520HostsProvider.DefaultUrl,
	string HostsRedirectMode = "DnsInterception",
	OllamaGatewaySettings? OllamaGateway = null) // "DnsInterception" or "HostsFile"
{
    internal static readonly string[] DefaultProcessNames =
	[
       // Visual Studio / IDE
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
        "code.exe",

		// Git
		"git.exe",
		"git-remote-http.exe",
		"git-remote-https.exe",
		"git-lfs.exe",

		// NuGet
		"nuget.exe",
		"dotnet.exe",

		// Docker
		"docker.exe",
		"docker-compose.exe",
		"docker desktop.exe",
		"com.docker.cli.exe",
		"com.docker.backend.exe",
		"com.docker.build.exe",
		"docker-credential-desktop.exe",

		// Microsoft 365 / WebView
		"m365copilot.exe",
		"m365copilot_autostarter.exe",
		"m365copilot_widget.exe",
		"webviewhost.exe"
	];

	internal static readonly string[] DefaultDomainRules =
	[
        // Copilot / GitHub
		"copilot.microsoft.com",
		"*.copilot.microsoft.com",
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

		// VS Code
		"vscode.dev",
		"*.vscode.dev",

		// NuGet
		"nuget.org",
		"*.nuget.org",
		"api.nuget.org",
		"globalcdn.nuget.org",

		// Docker
		"docker.com",
		"*.docker.com",
		"docker.io",
		"*.docker.io",
		"auth.docker.io",
		"hub.docker.com",
		"index.docker.io",
		"registry-1.docker.io",
		"production.cloudflare.docker.com"
	];

	internal static readonly string[] DefaultRefreshDomains =
	[
     // GitHub / Copilot
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
		"education.github.com",
		"private-user-images.githubusercontent.com",

		// VS Code
		"vscode.dev",

		// NuGet
		"api.nuget.org",
		"globalcdn.nuget.org",
		"nuget.org",
		"www.nuget.org",

		// Docker
		"auth.docker.io",
		"index.docker.io",
		"registry-1.docker.io",
		"production.cloudflare.docker.com",
		"hub.docker.com",
		"desktop.docker.com",
	];

	public static ProxyOptions? Parse(string[] args)
	{
		var processNames = new HashSet<string>(DefaultProcessNames.Select(TargetRuleNormalizer.NormalizeProcessName), StringComparer.OrdinalIgnoreCase);
		var domains = new HashSet<string>(DefaultDomainRules.Select(TargetRuleNormalizer.NormalizeDomain), StringComparer.OrdinalIgnoreCase);
		string host = "host.docker.internal";
		ushort port = 7890;
		string scheme = "socks5";

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

// ════════════════════════════════════════════════════════════════
//  Windows Credential Manager helper (advapi32 P/Invoke)
// ════════════════════════════════════════════════════════════════

internal static class CredentialManager
{
	private const int CredTypeGeneric = 1;
	private const int CredPersistLocalMachine = 2;

	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
	private struct NativeCredential
	{
		public uint Flags;
		public uint Type;
		public IntPtr TargetName;
		public IntPtr Comment;
		public System.Runtime.InteropServices.ComTypes.FILETIME LastWritten;
		public uint CredentialBlobSize;
		public IntPtr CredentialBlob;
		public uint Persist;
		public uint AttributeCount;
		public IntPtr Attributes;
		public IntPtr TargetAlias;
		public IntPtr UserName;
	}

	[DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool CredWriteW(ref NativeCredential userCredential, uint flags);

	[DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool CredReadW(string target, uint type, uint reservedFlag, out IntPtr credentialPtr);

	[DllImport("advapi32.dll", SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool CredDeleteW([MarshalAs(UnmanagedType.LPWStr)] string target, uint type, uint flags);

	[DllImport("advapi32.dll", SetLastError = true)]
	private static extern void CredFree(IntPtr cred);

	/// <summary>Stores a secret in Windows Credential Manager under the given target name.</summary>
	public static void SaveToken(string targetName, string token)
	{
		ArgumentNullException.ThrowIfNull(targetName);
		ArgumentNullException.ThrowIfNull(token);

		byte[] blob = Encoding.Unicode.GetBytes(token);

		var cred = new NativeCredential
		{
			Type = CredTypeGeneric,
			TargetName = Marshal.StringToCoTaskMemUni(targetName),
			CredentialBlobSize = (uint)blob.Length,
			CredentialBlob = Marshal.AllocCoTaskMem(blob.Length),
			Persist = CredPersistLocalMachine,
			UserName = Marshal.StringToCoTaskMemUni(Environment.UserName)
		};

		try
		{
			Marshal.Copy(blob, 0, cred.CredentialBlob, blob.Length);
			if (!CredWriteW(ref cred, 0))
				throw new InvalidOperationException(
					$"CredWriteW failed with error {Marshal.GetLastWin32Error()}");
		}
		finally
		{
			Marshal.FreeCoTaskMem(cred.TargetName);
			Marshal.FreeCoTaskMem(cred.CredentialBlob);
			Marshal.FreeCoTaskMem(cred.UserName);
		}
	}

	/// <summary>Reads a secret from Windows Credential Manager; returns <see langword="null"/> if not found.</summary>
	public static string? LoadToken(string targetName)
	{
		ArgumentNullException.ThrowIfNull(targetName);

		if (!CredReadW(targetName, CredTypeGeneric, 0, out IntPtr credPtr))
			return null;

		try
		{
			var native = Marshal.PtrToStructure<NativeCredential>(credPtr);
			if (native.CredentialBlobSize == 0 || native.CredentialBlob == IntPtr.Zero)
				return string.Empty;

			byte[] blob = new byte[native.CredentialBlobSize];
			Marshal.Copy(native.CredentialBlob, blob, 0, blob.Length);
			return Encoding.Unicode.GetString(blob);
		}
		finally
		{
			CredFree(credPtr);
		}
	}

	/// <summary>Deletes a stored credential; silently succeeds if it does not exist.</summary>
	public static void DeleteToken(string targetName)
	{
		ArgumentNullException.ThrowIfNull(targetName);
		CredDeleteW(targetName, CredTypeGeneric, 0);
	}

	/// <summary>Returns the Credential Manager target name for a given sync provider.</summary>
	public static string GetTargetName(string provider)
	{
		return GetConfigSyncTokenTargetName(provider);
	}

	public static string GetConfigSyncTokenTargetName(string provider)
	{
		ArgumentNullException.ThrowIfNull(provider);
		ValidateSyncProvider(provider);
		return $"TrafficPilot_ConfigSync_{provider}_Token";
	}

	public static string GetConfigSyncRemoteIdTargetName(string provider)
	{
		ArgumentNullException.ThrowIfNull(provider);
		ValidateSyncProvider(provider);
		return $"TrafficPilot_ConfigSync_{provider}_RemoteId";
	}

	public static void SaveConfigSyncToken(string provider, string token)
	{
		SaveToken(GetConfigSyncTokenTargetName(provider), token);
	}

	public static string? LoadConfigSyncToken(string provider)
	{
		var token = LoadToken(GetConfigSyncTokenTargetName(provider));
		if (token is not null)
			return token;

		var legacyTargetName = GetLegacyConfigSyncTargetName(provider);
		var legacyToken = LoadToken(legacyTargetName);
		if (legacyToken is null)
			return null;

		SaveToken(GetConfigSyncTokenTargetName(provider), legacyToken);
		DeleteToken(legacyTargetName);
		return legacyToken;
	}

	public static void DeleteConfigSyncToken(string provider)
	{
		DeleteToken(GetConfigSyncTokenTargetName(provider));
		DeleteToken(GetLegacyConfigSyncTargetName(provider));
	}

	public static void SaveConfigSyncRemoteId(string provider, string remoteId)
	{
		SaveToken(GetConfigSyncRemoteIdTargetName(provider), remoteId);
	}

	public static string? LoadConfigSyncRemoteId(string provider)
	{
		return LoadToken(GetConfigSyncRemoteIdTargetName(provider));
	}

	public static void DeleteConfigSyncRemoteId(string provider)
	{
		DeleteToken(GetConfigSyncRemoteIdTargetName(provider));
	}

	private static void ValidateSyncProvider(string provider)
	{
		if (!provider.Equals("GitHub", StringComparison.Ordinal)
			&& !provider.Equals("Gitee", StringComparison.Ordinal))
		{
			throw new ArgumentException($"Unknown sync provider: {provider}", nameof(provider));
		}
	}

	private static string GetLegacyConfigSyncTargetName(string provider)
	{
		ValidateSyncProvider(provider);
		return $"TrafficPilot_ConfigSync_{provider}";
	}

	public static string GetLocalApiTargetName(string providerName)
	{
		if (string.IsNullOrWhiteSpace(providerName))
			return "TrafficPilot_LocalApi_Default";

		var normalized = Regex.Replace(providerName.Trim(), @"[^A-Za-z0-9_-]+", "_");
		normalized = normalized.Trim('_');
		if (normalized.Length == 0)
			normalized = "Default";

		return $"TrafficPilot_LocalApi_{normalized}";
	}
}
