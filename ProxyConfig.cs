using System.Text.Json;
using System.Text.Json.Serialization;
using System.Diagnostics;

namespace TrafficPilot;

// ════════════════════════════════════════════════════════════════
//  JSON Configuration Models
// ════════════════════════════════════════════════════════════════

internal class ProxyConfigModel
{
	[JsonPropertyName("configName")]
	public string ConfigName { get; set; } = string.Empty;

	[JsonPropertyName("proxy")]
	public ProxySettings? Proxy { get; set; }

	[JsonPropertyName("targeting")]
	public TargetingSettings? Targeting { get; set; }

	[JsonPropertyName("hostsRedirect")]
	public HostsRedirectSettings? HostsRedirect { get; set; }

	[JsonPropertyName("startOnBoot")]
	public bool StartOnBoot { get; set; } = false;

	[JsonPropertyName("autoStartProxy")]
	public bool AutoStartProxy { get; set; } = false;

	public ProxyConfigModel() { }

	public ProxyConfigModel(ProxyOptions opts)
	{
		ConfigName = string.Empty;
		Proxy = new ProxySettings
		{
			Enabled = opts.ProxyEnabled,
			Host = opts.ProxyHost,
			Port = opts.ProxyPort,
			Scheme = opts.ProxyScheme
		};
		Targeting = new TargetingSettings
		{
			ProcessNames = opts.ProcessNames.ToList(),
			DomainRules = opts.DomainRules.ToList()
		};
		HostsRedirect = new HostsRedirectSettings
		{
			Enabled = opts.HostsRedirectEnabled,
			HostsUrl = opts.HostsRedirectUrl
		};
	}
}

internal class ProxySettings
{
	[JsonPropertyName("enabled")]
	public bool Enabled { get; set; } = true;

	[JsonPropertyName("host")]
	public string Host { get; set; } = "host.docker.internal";

	[JsonPropertyName("port")]
	public uint  Port { get; set; } = 7890;

	[JsonPropertyName("scheme")]
	public string Scheme { get; set; } = "socks4";
}

internal class TargetingSettings
{
	[JsonPropertyName("processNames")]
	public List<string> ProcessNames { get; set; } = [];

	[JsonPropertyName("domainRules")]
	public List<string> DomainRules { get; set; } = [];
}

internal class HostsRedirectSettings
{
	[JsonPropertyName("enabled")]
	public bool Enabled { get; set; } = false;

	[JsonPropertyName("hostsUrl")]
	public string HostsUrl { get; set; } = GitHub520HostsProvider.DefaultUrl;

	[JsonPropertyName("refreshDomains")]
	public List<string> RefreshDomains { get; set; } = [];
}

internal sealed class ProxyConfigManager
{
	private readonly string _configPath;

	public ProxyConfigManager(string? configPath = null)
	{
		_configPath = configPath ?? Path.Combine(
			Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
			"TrafficPilot",
			"config.json");
	}

	public ProxyConfigModel Load(string? configPath = null)
	{
		var path = Path.GetFullPath(configPath ?? _configPath);

		if (!File.Exists(path))
			return CreateDefaultConfig();

		try
		{
			var json = File.ReadAllText(path);
			var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
			return JsonSerializer.Deserialize<ProxyConfigModel>(json, options) ?? new ProxyConfigModel();
		}
		catch (IOException ex)
		{
			MessageBox.Show($"Failed to load config: {ex.Message}", "Load Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			return new ProxyConfigModel();
		}
		catch (UnauthorizedAccessException ex)
		{
			MessageBox.Show($"Failed to load config: {ex.Message}", "Load Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			return new ProxyConfigModel();
		}
		catch (JsonException ex)
		{
			MessageBox.Show($"Failed to load config: {ex.Message}", "Load Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			return new ProxyConfigModel();
		}
		catch (NotSupportedException ex)
		{
			MessageBox.Show($"Failed to load config: {ex.Message}", "Load Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			return new ProxyConfigModel();
		}
	}

	public void Save(ProxyConfigModel config, string? configPath = null)
	{
		ArgumentNullException.ThrowIfNull(config);

		var path = Path.GetFullPath(configPath ?? _configPath);
		var dir = Path.GetDirectoryName(path);

		try
		{
			if (!Directory.Exists(dir))
				Directory.CreateDirectory(dir!);

			var options = new JsonSerializerOptions
			{
				WriteIndented = true,
				PropertyNamingPolicy = JsonNamingPolicy.CamelCase
			};
			var json = JsonSerializer.Serialize(config, options);
			File.WriteAllText(path, json);
		}
		catch (IOException ex)
		{
			MessageBox.Show($"Failed to save config: {ex.Message}", "Save Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
		}
		catch (UnauthorizedAccessException ex)
		{
			MessageBox.Show($"Failed to save config: {ex.Message}", "Save Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
		}
		catch (NotSupportedException ex)
		{
			MessageBox.Show($"Failed to save config: {ex.Message}", "Save Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
		}
	}

	public string GetConfigDirectory() => Path.GetDirectoryName(_configPath)!;

	public IReadOnlyList<string> GetConfigPaths(int maxCount)
	{
		if (maxCount <= 0)
			return [];

		var configDirectory = GetConfigDirectory();
		if (!Directory.Exists(configDirectory))
			return [];

		return Directory
			.EnumerateFiles(configDirectory, "*.json", SearchOption.TopDirectoryOnly)
			.OrderByDescending(File.GetLastWriteTimeUtc)
			.Take(maxCount)
			.ToList();
	}

	public string GetConfigPath() => _configPath;

	public string GetConfigDisplayName(string configPath)
	{
		if (string.IsNullOrWhiteSpace(configPath))
			return ConfigDisplayNames.DefaultName;

		var path = Path.GetFullPath(configPath);
		if (!File.Exists(path))
			return ConfigDisplayNames.DefaultName;

		try
		{
			var json = File.ReadAllText(path);
			var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
			var config = JsonSerializer.Deserialize<ProxyConfigModel>(json, options);
			return ConfigDisplayNames.Normalize(config?.ConfigName);
		}
		catch (IOException ex)
		{
			Debug.WriteLine($"Failed to read config display name from '{path}': {ex.Message}");
			return ConfigDisplayNames.DefaultName;
		}
		catch (UnauthorizedAccessException ex)
		{
			Debug.WriteLine($"Failed to read config display name from '{path}': {ex.Message}");
			return ConfigDisplayNames.DefaultName;
		}
		catch (JsonException ex)
		{
			Debug.WriteLine($"Failed to read config display name from '{path}': {ex.Message}");
			return ConfigDisplayNames.DefaultName;
		}
		catch (NotSupportedException ex)
		{
			Debug.WriteLine($"Failed to read config display name from '{path}': {ex.Message}");
			return ConfigDisplayNames.DefaultName;
		}
	}

	private static ProxyConfigModel CreateDefaultConfig()
	{
		return new ProxyConfigModel
		{
            Proxy = new ProxySettings(),
            Targeting = new TargetingSettings
            {
                ProcessNames = [.. ProxyOptions.DefaultProcessNames],
                DomainRules = [.. ProxyOptions.DefaultDomainRules]
            },
            HostsRedirect = new HostsRedirectSettings
            {
                RefreshDomains = [.. ProxyOptions.DefaultRefreshDomains]
			}
		};
	}
}

internal static class ConfigDisplayNames
{
	internal const string DefaultName = "Default";

	internal static string Normalize(string? configName)
	{
		return string.IsNullOrWhiteSpace(configName)
			? DefaultName
			: configName.Trim();
	}
}
