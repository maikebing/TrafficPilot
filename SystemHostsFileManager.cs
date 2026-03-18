using System.ComponentModel;
using System.Diagnostics;
using System.Text;

namespace TrafficPilot;

// ════════════════════════════════════════════════════════════════
//  System Hosts File Manager
// ════════════════════════════════════════════════════════════════

/// <summary>
/// Manages reading and writing the system hosts file (C:\Windows\System32\drivers\etc\hosts)
/// with safe handling of TrafficPilot-managed entries.
/// </summary>
internal static class SystemHostsFileManager
{
	private const string HostsFilePath = @"C:\Windows\System32\drivers\etc\hosts";
	private const string ManagedSectionStart = "# ========== TrafficPilot Managed Section - Start ==========";
	private const string ManagedSectionEnd = "# ========== TrafficPilot Managed Section - End ==========";
	private const int WslCommandTimeoutMs = 5000;

	/// <summary>
	/// Reads the hosts file and returns user entries (excluding TrafficPilot-managed section)
	/// and TrafficPilot entries separately.
	/// </summary>
	public static (List<string> userEntries, Dictionary<string, string> trafficPilotEntries) ReadHostsFile()
	{
		try
		{
           return ParseHostsLines(ReadHostsLines());
		}
		catch (UnauthorizedAccessException)
		{
			throw new InvalidOperationException("Access denied. Please run TrafficPilot as Administrator to modify the hosts file.");
		}
		catch (Exception ex)
		{
			throw new InvalidOperationException($"Failed to read hosts file: {ex.Message}", ex);
		}
	}

	/// <summary>
	/// Writes the hosts file with user entries preserved and TrafficPilot entries in a managed section.
	/// </summary>
	/// <param name="hostsMap">Dictionary of domain -> IP mappings to be managed by TrafficPilot</param>
	public static void WriteHostsFile(Dictionary<string, byte[]> hostsMap)
	{
     ArgumentNullException.ThrowIfNull(hostsMap);

		try
		{
         WriteHostsFileCore(hostsMap);

			foreach (var distributionName in GetWslDistributionNames())
			{
				try
				{
					WriteHostsFileCore(hostsMap, distributionName);
				}
				catch (Exception ex)
				{
					Debug.WriteLine($"[hosts] Failed to sync WSL hosts for '{distributionName}': {ex.Message}");
				}
			}
		}
		catch (UnauthorizedAccessException)
		{
			throw new InvalidOperationException("Access denied. Please run TrafficPilot as Administrator to modify the hosts file.");
		}
		catch (Exception ex)
		{
			throw new InvalidOperationException($"Failed to write hosts file: {ex.Message}", ex);
		}
	}

	/// <summary>
	/// Removes all TrafficPilot-managed entries from the hosts file, restoring only user entries.
	/// </summary>
	public static void RemoveTrafficPilotEntries()
	{
		try
		{
         RemoveTrafficPilotEntriesCore();

			foreach (var distributionName in GetWslDistributionNames())
			{
				try
				{
					RemoveTrafficPilotEntriesCore(distributionName);
				}
				catch (Exception ex)
				{
					Debug.WriteLine($"[hosts] Failed to clean WSL hosts for '{distributionName}': {ex.Message}");
				}
			}
		}
		catch (UnauthorizedAccessException)
		{
			throw new InvalidOperationException("Access denied. Please run TrafficPilot as Administrator to modify the hosts file.");
		}
		catch (Exception ex)
		{
			throw new InvalidOperationException($"Failed to remove TrafficPilot entries: {ex.Message}", ex);
		}
	}

	/// <summary>
	/// Checks if the application has write access to the hosts file.
	/// </summary>
	public static bool HasWriteAccess()
	{
		try
		{
			// Try to open the file for writing
			using var fs = File.Open(HostsFilePath, FileMode.Open, FileAccess.Write);
			return true;
		}
		catch (UnauthorizedAccessException)
		{
			return false;
		}
		catch (Exception)
		{
			return false;
		}
	}

	/// <summary>
	/// Creates a backup of the current hosts file.
	/// </summary>
	private static void CreateBackup()
	{
        CreateBackup(null, ReadHostsLines());
	}

	/// <summary>
	/// Flushes the DNS cache to ensure hosts file changes take effect immediately.
	/// </summary>
	public static void FlushDnsCache()
	{
		try
		{
			var psi = new System.Diagnostics.ProcessStartInfo
			{
				FileName = "ipconfig",
				Arguments = "/flushdns",
				UseShellExecute = false,
				CreateNoWindow = true,
				RedirectStandardOutput = true,
				RedirectStandardError = true
			};

			using var process = System.Diagnostics.Process.Start(psi);
			process?.WaitForExit(5000);
		}
		catch
		{
			// DNS flush failure is not critical
		}
	}

	private static void WriteHostsFileCore(Dictionary<string, byte[]> hostsMap, string? distributionName = null)
	{
		var existingLines = ReadHostsLines(distributionName);
		var (userEntries, _) = ParseHostsLines(existingLines);
		var content = BuildHostsFileContent(userEntries, hostsMap);

		CreateBackup(distributionName, existingLines);
		WriteHostsContent(content, distributionName);
	}

	private static void RemoveTrafficPilotEntriesCore(string? distributionName = null)
	{
		var existingLines = ReadHostsLines(distributionName);
		var (userEntries, _) = ParseHostsLines(existingLines);

		CreateBackup(distributionName, existingLines);
		WriteHostsContent(BuildHostsFileContent(userEntries, []), distributionName);
	}

	private static string[] ReadHostsLines(string? distributionName = null)
	{
		if (string.IsNullOrWhiteSpace(distributionName))
			return File.Exists(HostsFilePath) ? File.ReadAllLines(HostsFilePath) : [];

		return ReadWslHostsLines(distributionName);
	}

	private static (List<string> userEntries, Dictionary<string, string> trafficPilotEntries) ParseHostsLines(IEnumerable<string> lines)
	{
		var userEntries = new List<string>();
		var trafficPilotEntries = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		bool inManagedSection = false;

		foreach (var line in lines)
		{
			if (line.Trim() == ManagedSectionStart)
			{
				inManagedSection = true;
				continue;
			}

			if (line.Trim() == ManagedSectionEnd)
			{
				inManagedSection = false;
				continue;
			}

			if (inManagedSection)
			{
				var trimmed = line.Trim();
				if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith('#'))
					continue;

				var parts = trimmed.Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
				if (parts.Length < 2)
					continue;

				var ip = parts[0];
				for (int i = 1; i < parts.Length; i++)
					trafficPilotEntries[parts[i]] = ip;
			}
			else
			{
				userEntries.Add(line);
			}
		}

		return (userEntries, trafficPilotEntries);
	}

	private static string BuildHostsFileContent(List<string> userEntries, Dictionary<string, byte[]> hostsMap)
	{
		var sb = new StringBuilder();

		foreach (var line in userEntries)
			sb.AppendLine(line);

		if (userEntries.Count > 0 && !string.IsNullOrWhiteSpace(userEntries[^1]) && hostsMap.Count > 0)
			sb.AppendLine();

		if (hostsMap.Count == 0)
			return sb.ToString();

		sb.AppendLine(ManagedSectionStart);
		sb.AppendLine($"# Last updated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
		sb.AppendLine($"# Total entries: {hostsMap.Count}");
		sb.AppendLine();

		var ipGroups = hostsMap
			.GroupBy(kvp => string.Join(".", kvp.Value))
			.OrderBy(g => g.Key);

		foreach (var group in ipGroups)
		{
			var ip = group.Key;
			var domainList = group.Select(kvp => kvp.Key).OrderBy(d => d).ToList();
			for (int i = 0; i < domainList.Count; i += 3)
			{
				var batch = domainList.Skip(i).Take(3);
				sb.AppendLine($"{ip,-15} {string.Join(" ", batch)}");
			}
		}

		sb.AppendLine();
		sb.AppendLine(ManagedSectionEnd);
		return sb.ToString();
	}

	private static void WriteHostsContent(string content, string? distributionName = null)
	{
		if (string.IsNullOrWhiteSpace(distributionName))
		{
			File.WriteAllText(HostsFilePath, content, Encoding.UTF8);
			return;
		}

		WriteWslHostsContent(distributionName, content);
	}

	private static IReadOnlyList<string> GetWslDistributionNames()
	{
		try
		{
			using var process = StartWslProcess([
				"--list",
				"--quiet"
			], redirectStandardInput: false);

			var output = process.StandardOutput.ReadToEnd();
			var error = process.StandardError.ReadToEnd();

			WaitForExit(process, "list WSL distributions");

			if (process.ExitCode != 0)
			{
				Debug.WriteLine($"[hosts] Failed to detect WSL distributions: {error}");
				return [];
			}

			return SplitLines(output)
				.Select(static line => line.Trim())
				.Where(static line => !string.IsNullOrWhiteSpace(line))
				.Where(static line => !line.StartsWith("docker-desktop", StringComparison.OrdinalIgnoreCase))
				.Distinct(StringComparer.OrdinalIgnoreCase)
				.ToArray();
		}
		catch (Win32Exception)
		{
			return [];
		}
		catch (InvalidOperationException)
		{
			return [];
		}
		catch (TimeoutException)
		{
			return [];
		}
	}

	private static string[] ReadWslHostsLines(string distributionName)
	{
		using var process = StartWslProcess([
			"--distribution",
			distributionName,
			"--user",
			"root",
			"--exec",
			"cat",
			"/etc/hosts"
		], redirectStandardInput: false);

		var output = process.StandardOutput.ReadToEnd();
		var error = process.StandardError.ReadToEnd();

		WaitForExit(process, $"read WSL hosts for '{distributionName}'");

		if (process.ExitCode != 0)
			throw new InvalidOperationException($"Failed to read WSL hosts for '{distributionName}': {error.Trim()}");

		return SplitLines(output);
	}

	private static void WriteWslHostsContent(string distributionName, string content)
	{
		using var process = StartWslProcess([
			"--distribution",
			distributionName,
			"--user",
			"root",
			"--exec",
			"tee",
			"/etc/hosts"
		], redirectStandardInput: true);

		process.StandardInput.Write(content);
		process.StandardInput.Close();

		_ = process.StandardOutput.ReadToEnd();
		var error = process.StandardError.ReadToEnd();

		WaitForExit(process, $"write WSL hosts for '{distributionName}'");

		if (process.ExitCode != 0)
			throw new InvalidOperationException($"Failed to write WSL hosts for '{distributionName}': {error.Trim()}");
	}

	private static Process StartWslProcess(IEnumerable<string> arguments, bool redirectStandardInput)
	{
		var startInfo = new ProcessStartInfo
		{
			FileName = "wsl.exe",
			UseShellExecute = false,
			CreateNoWindow = true,
			RedirectStandardInput = redirectStandardInput,
			RedirectStandardOutput = true,
			RedirectStandardError = true
		};

		foreach (var argument in arguments)
			startInfo.ArgumentList.Add(argument);

		return Process.Start(startInfo)
			?? throw new InvalidOperationException("Failed to start wsl.exe.");
	}

	private static void WaitForExit(Process process, string operation)
	{
		if (process.WaitForExit(WslCommandTimeoutMs))
			return;

		try
		{
			process.Kill(entireProcessTree: true);
		}
		catch (InvalidOperationException)
		{
		}

		throw new TimeoutException($"Timed out while attempting to {operation}.");
	}

	private static string[] SplitLines(string content)
	{
		if (string.IsNullOrEmpty(content))
			return [];

		var lines = new List<string>();
		using var reader = new StringReader(content);
		while (reader.ReadLine() is { } line)
			lines.Add(line);

		return [.. lines];
	}

	private static void CreateBackup(string? distributionName, IReadOnlyCollection<string> existingLines)
	{
		if (existingLines.Count == 0)
			return;

		try
		{
			var backupDir = Path.Combine(
				Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
				"TrafficPilot",
				"HostsBackups");

			Directory.CreateDirectory(backupDir);

			var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
			var targetName = SanitizeBackupName(distributionName ?? "windows");
			var backupPath = Path.Combine(backupDir, $"{targetName}_hosts_backup_{timestamp}.txt");

			File.WriteAllText(backupPath, string.Join(Environment.NewLine, existingLines), Encoding.UTF8);

			var backups = Directory.GetFiles(backupDir, $"{targetName}_hosts_backup_*.txt")
				.OrderByDescending(static f => f)
				.Skip(10)
				.ToList();

			foreach (var oldBackup in backups)
			{
				try { File.Delete(oldBackup); } catch { }
			}
		}
		catch
		{
		}
	}

	private static string SanitizeBackupName(string name)
	{
		var invalidChars = Path.GetInvalidFileNameChars();
		var sanitized = new string(name.Select(ch => invalidChars.Contains(ch) ? '_' : ch).ToArray());
		return string.IsNullOrWhiteSpace(sanitized) ? "hosts" : sanitized;
	}
}
