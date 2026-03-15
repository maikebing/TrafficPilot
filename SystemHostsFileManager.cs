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

	/// <summary>
	/// Reads the hosts file and returns user entries (excluding TrafficPilot-managed section)
	/// and TrafficPilot entries separately.
	/// </summary>
	public static (List<string> userEntries, Dictionary<string, string> trafficPilotEntries) ReadHostsFile()
	{
		var userEntries = new List<string>();
		var trafficPilotEntries = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

		if (!File.Exists(HostsFilePath))
			return (userEntries, trafficPilotEntries);

		try
		{
			var lines = File.ReadAllLines(HostsFilePath);
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
					// Parse TrafficPilot managed entries
					var trimmed = line.Trim();
					if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith('#'))
						continue;

					var parts = trimmed.Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
					if (parts.Length >= 2)
					{
						var ip = parts[0];
						// Add all domains mapped to this IP
						for (int i = 1; i < parts.Length; i++)
						{
							trafficPilotEntries[parts[i]] = ip;
						}
					}
				}
				else
				{
					// Preserve user entries
					userEntries.Add(line);
				}
			}
		}
		catch (UnauthorizedAccessException)
		{
			throw new InvalidOperationException("Access denied. Please run TrafficPilot as Administrator to modify the hosts file.");
		}
		catch (Exception ex)
		{
			throw new InvalidOperationException($"Failed to read hosts file: {ex.Message}", ex);
		}

		return (userEntries, trafficPilotEntries);
	}

	/// <summary>
	/// Writes the hosts file with user entries preserved and TrafficPilot entries in a managed section.
	/// </summary>
	/// <param name="hostsMap">Dictionary of domain -> IP mappings to be managed by TrafficPilot</param>
	public static void WriteHostsFile(Dictionary<string, byte[]> hostsMap)
	{
		try
		{
			var (userEntries, _) = ReadHostsFile();

			var sb = new StringBuilder();

			// Write user entries first
			foreach (var line in userEntries)
			{
				sb.AppendLine(line);
			}

			// Ensure there's a blank line before our section
			if (userEntries.Count > 0 && !string.IsNullOrWhiteSpace(userEntries[^1]))
			{
				sb.AppendLine();
			}

			// Write TrafficPilot managed section
			if (hostsMap.Count > 0)
			{
				sb.AppendLine(ManagedSectionStart);
				sb.AppendLine($"# Last updated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
				sb.AppendLine($"# Total entries: {hostsMap.Count}");
				sb.AppendLine();

				// Group by IP for cleaner output
				var ipGroups = hostsMap
					.GroupBy(kvp => string.Join(".", kvp.Value))
					.OrderBy(g => g.Key);

				foreach (var group in ipGroups)
				{
					var ip = group.Key;
					var domains = group.Select(kvp => kvp.Key).OrderBy(d => d);
					
					// Write multiple domains per line (max 3 for readability)
					var domainList = domains.ToList();
					for (int i = 0; i < domainList.Count; i += 3)
					{
						var batch = domainList.Skip(i).Take(3);
						sb.AppendLine($"{ip,-15} {string.Join(" ", batch)}");
					}
				}

				sb.AppendLine();
				sb.AppendLine(ManagedSectionEnd);
			}

			// Create backup before writing
			CreateBackup();

			// Write to hosts file
			File.WriteAllText(HostsFilePath, sb.ToString(), Encoding.UTF8);
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
			var (userEntries, _) = ReadHostsFile();

			// Create backup before modifying
			CreateBackup();

			// Write back only user entries
			File.WriteAllLines(HostsFilePath, userEntries, Encoding.UTF8);
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
		if (!File.Exists(HostsFilePath))
			return;

		try
		{
			var backupDir = Path.Combine(
				Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
				"TrafficPilot",
				"HostsBackups");

			Directory.CreateDirectory(backupDir);

			var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
			var backupPath = Path.Combine(backupDir, $"hosts_backup_{timestamp}.txt");

			File.Copy(HostsFilePath, backupPath, overwrite: true);

			// Keep only last 10 backups
			var backups = Directory.GetFiles(backupDir, "hosts_backup_*.txt")
				.OrderByDescending(f => f)
				.Skip(10)
				.ToList();

			foreach (var oldBackup in backups)
			{
				try { File.Delete(oldBackup); } catch { /* ignore */ }
			}
		}
		catch
		{
			// Backup failure shouldn't prevent the main operation
		}
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
}
