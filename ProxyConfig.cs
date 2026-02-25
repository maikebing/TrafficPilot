using System.Text.Json;
using System.Text.Json.Serialization;

namespace VSifier;

// ════════════════════════════════════════════════════════════════
//  JSON Configuration Models
// ════════════════════════════════════════════════════════════════

internal class ProxyConfigModel
{
	[JsonPropertyName("proxy")]
	public ProxySettings? Proxy { get; set; }

	[JsonPropertyName("targeting")]
	public TargetingSettings? Targeting { get; set; }

	public ProxyConfigModel() { }

	public ProxyConfigModel(ProxyOptions opts)
	{
		Proxy = new ProxySettings
		{
			Host = opts.ProxyHost,
			Port = opts.ProxyPort,
			Scheme = opts.ProxyScheme
		};
		Targeting = new TargetingSettings
		{
			ProcessNames = opts.ProcessNames.ToList(),
			ExtraPids = opts.ExtraPids.ToList()
		};
	}
}

internal class ProxySettings
{
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

	[JsonPropertyName("extraPids")]
	public List<int> ExtraPids { get; set; } = [];
}

internal sealed class ProxyConfigManager
{
	private readonly string _configPath;

	public ProxyConfigManager(string? configPath = null)
	{
		_configPath = configPath ?? Path.Combine(
			Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
			"VSifier",
			"config.json");
	}

	public ProxyConfigModel Load()
	{
		if (!File.Exists(_configPath))
			return new ProxyConfigModel
			{
				Proxy = new ProxySettings(),
				Targeting = new TargetingSettings
				{
					ProcessNames = new List<string>
					{
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
					}
				}
			};

		try
		{
			var json = File.ReadAllText(_configPath);
			var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
			return JsonSerializer.Deserialize<ProxyConfigModel>(json, options) ?? new ProxyConfigModel();
		}
		catch (Exception ex)
		{
			MessageBox.Show($"Failed to load config: {ex.Message}", "Load Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			return new ProxyConfigModel();
		}
	}

	public void Save(ProxyConfigModel config)
	{
		try
		{
			var dir = Path.GetDirectoryName(_configPath);
			if (!Directory.Exists(dir))
				Directory.CreateDirectory(dir!);

			var options = new JsonSerializerOptions
			{
				WriteIndented = true,
				PropertyNamingPolicy = JsonNamingPolicy.CamelCase
			};
			var json = JsonSerializer.Serialize(config, options);
			File.WriteAllText(_configPath, json);
		}
		catch (Exception ex)
		{
			MessageBox.Show($"Failed to save config: {ex.Message}", "Save Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
		}
	}

	public string GetConfigPath() => _configPath;
}
