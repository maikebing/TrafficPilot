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

	[JsonPropertyName("configSync")]
	public ConfigSyncSettings? ConfigSync { get; set; }

	[JsonPropertyName("ollamaGateway")]
	public OllamaGatewaySettings? OllamaGateway { get; set; }

	[JsonPropertyName("localApiForwarder")]
	public LocalApiForwarderSettings? LocalApiForwarder { get; set; }

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
		OllamaGateway = opts.OllamaGateway;
		LocalApiForwarder = opts.LocalApiForwarder;
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
	public string Scheme { get; set; } = "socks5";

	[JsonPropertyName("isLocalProxy")]
	public bool IsLocalProxy { get; set; } = false;
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

	[JsonPropertyName("mode")]
	public string Mode { get; set; } = "DnsInterception"; // "DnsInterception" or "HostsFile"

	[JsonPropertyName("hostsUrl")]
	public string HostsUrl { get; set; } = GitHub520HostsProvider.DefaultUrl;

	[JsonPropertyName("refreshDomains")]
	public List<string> RefreshDomains { get; set; } = [];
}

internal class ConfigSyncSettings
{
	[JsonPropertyName("provider")]
	public string Provider { get; set; } = "GitHub"; // "GitHub" or "Gitee"

	[JsonPropertyName("gistId")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? GistId { get; set; } // Remote gist / snippet ID — not sensitive, stored in config
}

internal class LocalApiForwarderSettings
{
	[JsonPropertyName("enabled")]
	public bool Enabled { get; set; } = true;

	[JsonPropertyName("ollamaPort")]
	public ushort OllamaPort { get; set; } = 11434;

	[JsonPropertyName("foundryPort")]
	public ushort FoundryPort { get; set; } = 5273;

	[JsonPropertyName("provider")]
	public LocalApiProviderSettings Provider { get; set; } = new();

	[JsonPropertyName("modelMappings")]
	public List<LocalApiModelMapping> ModelMappings { get; set; } = [];

	[JsonPropertyName("requestResponseLogging")]
	public LocalApiRequestResponseLoggingSettings RequestResponseLogging { get; set; } = new();

	[JsonPropertyName("includeErrorDiagnostics")]
	public bool IncludeErrorDiagnostics { get; set; } = true;
}

internal class OllamaGatewaySettings
{
	[JsonPropertyName("enabled")]
	public bool Enabled { get; set; } = true;

	[JsonPropertyName("ollamaPort")]
	public ushort OllamaPort { get; set; } = 11434;

	[JsonPropertyName("openAiPort")]
	public ushort OpenAiPort { get; set; } = 5273;

	[JsonPropertyName("providers")]
	public List<GatewayProviderSettings> Providers { get; set; } = [];

	[JsonPropertyName("routes")]
	public List<GatewayRouteSettings> Routes { get; set; } = [];

	[JsonPropertyName("requestResponseLogging")]
	public LocalApiRequestResponseLoggingSettings RequestResponseLogging { get; set; } = new();

	[JsonPropertyName("includeErrorDiagnostics")]
	public bool IncludeErrorDiagnostics { get; set; } = true;

	public GatewayProviderSettings? GetDefaultProvider()
	{
		return Providers.FirstOrDefault(static provider => provider.Enabled)
			?? Providers.FirstOrDefault();
	}
}

internal class GatewayProviderSettings
{
	[JsonPropertyName("id")]
	public string Id { get; set; } = "default";

	[JsonPropertyName("enabled")]
	public bool Enabled { get; set; } = true;

	[JsonPropertyName("protocol")]
	public string Protocol { get; set; } = "OpenAICompatible";

	[JsonPropertyName("name")]
	public string Name { get; set; } = "Default Provider";

	[JsonPropertyName("baseUrl")]
	public string BaseUrl { get; set; } = "https://api.openai.com/v1/";

	[JsonPropertyName("defaultModel")]
	public string DefaultModel { get; set; } = string.Empty;

	[JsonPropertyName("defaultEmbeddingModel")]
	public string DefaultEmbeddingModel { get; set; } = string.Empty;

	[JsonPropertyName("cachedModels")]
	public List<string> CachedModels { get; set; } = [];

	[JsonPropertyName("cachedEmbeddingModels")]
	public List<string> CachedEmbeddingModels { get; set; } = [];

	[JsonPropertyName("cachedMessageModels")]
	public List<string> CachedMessageModels { get; set; } = [];

	[JsonPropertyName("cachedOtherModels")]
	public List<string> CachedOtherModels { get; set; } = [];

	[JsonPropertyName("cachedModerationModels")]
	public List<string> CachedModerationModels { get; set; } = [];

	[JsonPropertyName("cachedUnknownModels")]
	public List<string> CachedUnknownModels { get; set; } = [];

	[JsonPropertyName("cachedModelSummaries")]
	public Dictionary<string, string> CachedModelSummaries { get; set; } = new(StringComparer.OrdinalIgnoreCase);

	[JsonPropertyName("authType")]
	public string AuthType { get; set; } = "Bearer";

	[JsonPropertyName("authHeaderName")]
	public string AuthHeaderName { get; set; } = "Authorization";

	[JsonPropertyName("chatEndpoint")]
	public string ChatEndpoint { get; set; } = "chat/completions";

	[JsonPropertyName("embeddingsEndpoint")]
	public string EmbeddingsEndpoint { get; set; } = "embeddings";

	[JsonPropertyName("responsesEndpoint")]
	public string ResponsesEndpoint { get; set; } = "responses";

	[JsonPropertyName("additionalHeaders")]
	public List<LocalApiHeaderSetting> AdditionalHeaders { get; set; } = [];

	[JsonPropertyName("capabilities")]
	public GatewayProviderCapabilitySettings Capabilities { get; set; } = new();
}

internal class GatewayProviderCapabilitySettings
{
	[JsonPropertyName("supportsChat")]
	public bool SupportsChat { get; set; } = true;

	[JsonPropertyName("supportsEmbeddings")]
	public bool SupportsEmbeddings { get; set; } = true;

	[JsonPropertyName("supportsResponses")]
	public bool SupportsResponses { get; set; } = true;

	[JsonPropertyName("supportsStreaming")]
	public bool SupportsStreaming { get; set; } = true;
}

internal class GatewayRouteSettings
{
	[JsonPropertyName("localModel")]
	public string LocalModel { get; set; } = string.Empty;

	[JsonPropertyName("providerId")]
	public string ProviderId { get; set; } = "default";

	[JsonPropertyName("upstreamModel")]
	public string UpstreamModel { get; set; } = string.Empty;
}

internal class LocalApiProviderSettings
{
	[JsonPropertyName("protocol")]
	public string Protocol { get; set; } = "OpenAICompatible";

	[JsonPropertyName("name")]
	public string Name { get; set; } = "OpenAI Compatible";

	[JsonPropertyName("baseUrl")]
	public string BaseUrl { get; set; } = "https://api.openai.com/v1/";

	[JsonPropertyName("defaultModel")]
	public string DefaultModel { get; set; } = string.Empty;

	[JsonPropertyName("defaultEmbeddingModel")]
	public string DefaultEmbeddingModel { get; set; } = string.Empty;

	[JsonPropertyName("authType")]
	public string AuthType { get; set; } = "Bearer";

	[JsonPropertyName("authHeaderName")]
	public string AuthHeaderName { get; set; } = "Authorization";

	[JsonPropertyName("chatEndpoint")]
	public string ChatEndpoint { get; set; } = "chat/completions";

	[JsonPropertyName("embeddingsEndpoint")]
	public string EmbeddingsEndpoint { get; set; } = "embeddings";

	[JsonPropertyName("responsesEndpoint")]
	public string ResponsesEndpoint { get; set; } = "responses";

	[JsonPropertyName("additionalHeaders")]
	public List<LocalApiHeaderSetting> AdditionalHeaders { get; set; } = [];
}

internal class LocalApiModelMapping
{
	[JsonPropertyName("localModel")]
	public string LocalModel { get; set; } = string.Empty;

	[JsonPropertyName("upstreamModel")]
	public string UpstreamModel { get; set; } = string.Empty;
}

internal class LocalApiHeaderSetting
{
	[JsonPropertyName("name")]
	public string Name { get; set; } = string.Empty;

	[JsonPropertyName("value")]
	public string Value { get; set; } = string.Empty;
}

internal class LocalApiRequestResponseLoggingSettings
{
	[JsonPropertyName("enabled")]
	public bool Enabled { get; set; } = false;

	[JsonPropertyName("includeBodies")]
	public bool IncludeBodies { get; set; } = false;

	[JsonPropertyName("maxBodyCharacters")]
	public int MaxBodyCharacters { get; set; } = 4000;
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
			var config = JsonSerializer.Deserialize<ProxyConfigModel>(json, options) ?? new ProxyConfigModel();
			var migrated = TryNormalizeLoadedConfig(config, json);
			if (migrated)
				Save(config, path);
			return config;
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
			},
			OllamaGateway = CreateDefaultGatewaySettings(),
			LocalApiForwarder = new LocalApiForwarderSettings()
		};
	}

	private static OllamaGatewaySettings CreateDefaultGatewaySettings()
	{
		return new OllamaGatewaySettings
		{
			Providers =
			[
				new GatewayProviderSettings
				{
					Id = "default",
					Enabled = true,
					Protocol = "OpenAICompatible",
					Name = "OpenAI Compatible",
					BaseUrl = "https://api.openai.com/v1/",
					Capabilities = new GatewayProviderCapabilitySettings
					{
						SupportsChat = true,
						SupportsEmbeddings = true,
						SupportsResponses = true,
						SupportsStreaming = true
					}
				}
			]
		};
	}

	private static bool TryNormalizeLoadedConfig(ProxyConfigModel config, string json)
	{
		ArgumentNullException.ThrowIfNull(config);

		var changed = false;
		using var document = JsonDocument.Parse(json);
		var root = document.RootElement;

		config.OllamaGateway ??= BuildGatewaySettingsFromLegacy(config.LocalApiForwarder);
		if (config.OllamaGateway is not null)
		{
			NormalizeGatewaySettings(config.OllamaGateway);
			changed = true;
		}

		if (!root.TryGetProperty("localApiForwarder", out var localApiElement))
		{
			config.LocalApiForwarder ??= new LocalApiForwarderSettings();
			return true;
		}

		config.LocalApiForwarder ??= new LocalApiForwarderSettings();
		var settings = config.LocalApiForwarder;
		if (settings is null)
			return changed;

		settings.Provider ??= new LocalApiProviderSettings();
		settings.RequestResponseLogging ??= new LocalApiRequestResponseLoggingSettings();
		settings.ModelMappings ??= [];

		if (!localApiElement.TryGetProperty("enabled", out _))
		{
			if (!settings.Enabled)
			{
				settings.Enabled = true;
				changed = true;
			}
		}
		else if (!settings.Enabled && LooksLikeLegacyLocalApiConfig(settings))
		{
			settings.Enabled = true;
			changed = true;
		}

		return changed;
	}

	internal static OllamaGatewaySettings BuildGatewaySettingsFromLegacy(LocalApiForwarderSettings? legacy)
	{
		legacy ??= new LocalApiForwarderSettings();

		var providerId = "default";
		var provider = legacy.Provider ?? new LocalApiProviderSettings();
		var capabilities = new GatewayProviderCapabilitySettings
		{
			SupportsChat = true,
			SupportsResponses = true,
			SupportsStreaming = !string.Equals(provider.Protocol, "Anthropic", StringComparison.OrdinalIgnoreCase),
			SupportsEmbeddings = !string.Equals(provider.Protocol, "Anthropic", StringComparison.OrdinalIgnoreCase)
		};

		return new OllamaGatewaySettings
		{
			Enabled = legacy.Enabled,
			OllamaPort = legacy.OllamaPort,
			OpenAiPort = legacy.FoundryPort,
			RequestResponseLogging = legacy.RequestResponseLogging ?? new LocalApiRequestResponseLoggingSettings(),
			IncludeErrorDiagnostics = legacy.IncludeErrorDiagnostics,
			Providers =
			[
				new GatewayProviderSettings
				{
					Id = providerId,
					Enabled = legacy.Enabled,
					Protocol = provider.Protocol,
					Name = provider.Name,
					BaseUrl = provider.BaseUrl,
					DefaultModel = provider.DefaultModel,
					DefaultEmbeddingModel = provider.DefaultEmbeddingModel,
					AuthType = provider.AuthType,
					AuthHeaderName = provider.AuthHeaderName,
					ChatEndpoint = provider.ChatEndpoint,
					EmbeddingsEndpoint = provider.EmbeddingsEndpoint,
					ResponsesEndpoint = provider.ResponsesEndpoint,
					AdditionalHeaders = provider.AdditionalHeaders ?? [],
					Capabilities = capabilities
				}
			],
			Routes = legacy.ModelMappings
				.Select(mapping => new GatewayRouteSettings
				{
					LocalModel = mapping.LocalModel,
					ProviderId = providerId,
					UpstreamModel = mapping.UpstreamModel
				})
				.ToList()
		};
	}

	private static void NormalizeGatewaySettings(OllamaGatewaySettings settings)
	{
		settings.Providers ??= [];
		settings.Routes ??= [];
		settings.RequestResponseLogging ??= new LocalApiRequestResponseLoggingSettings();

		foreach (var provider in settings.Providers)
		{
			provider.Capabilities ??= new GatewayProviderCapabilitySettings();
			provider.AdditionalHeaders ??= [];
			if (string.IsNullOrWhiteSpace(provider.Id))
				provider.Id = BuildProviderId(provider.Name, provider.Protocol);
		}
	}

	private static string BuildProviderId(string? name, string? protocol)
	{
		var source = string.IsNullOrWhiteSpace(name) ? protocol : name;
		if (string.IsNullOrWhiteSpace(source))
			return "default";

		var normalized = new string(source
			.Trim()
			.ToLowerInvariant()
			.Select(static ch => char.IsLetterOrDigit(ch) ? ch : '-')
			.ToArray())
			.Trim('-');

		return string.IsNullOrWhiteSpace(normalized) ? "default" : normalized;
	}

	private static bool LooksLikeLegacyLocalApiConfig(LocalApiForwarderSettings settings)
	{
		var providerDomain = TryGetProviderDomain(settings.Provider?.BaseUrl);
		if (string.IsNullOrWhiteSpace(providerDomain))
			return !string.IsNullOrWhiteSpace(settings.Provider?.BaseUrl);

		return NeedsDisplaySuffixMigration(settings.Provider?.DefaultModel, GetProviderSuffixCandidates(providerDomain))
			|| NeedsDisplaySuffixMigration(settings.Provider?.DefaultEmbeddingModel, GetProviderSuffixCandidates(providerDomain))
			|| settings.ModelMappings.Any(static mapping => !string.IsNullOrWhiteSpace(mapping.UpstreamModel));
	}

	private static bool NeedsDisplaySuffixMigration(string? modelName, IEnumerable<string> providerSuffixes)
	{
		if (string.IsNullOrWhiteSpace(modelName))
			return false;

		var trimmedModel = modelName.Trim();
		return !providerSuffixes.Any(suffix => trimmedModel.EndsWith($"@{suffix}", StringComparison.OrdinalIgnoreCase));
	}

	private static string TryGetProviderDomain(string? baseUrl)
	{
		if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri))
			return string.Empty;

		return uri.IdnHost.Trim().TrimEnd('.').ToLowerInvariant();
	}

	private static IEnumerable<string> GetProviderSuffixCandidates(string providerDomain)
	{
		if (string.IsNullOrWhiteSpace(providerDomain))
			yield break;

		yield return providerDomain;

		var compact = providerDomain.Replace(".", string.Empty, StringComparison.Ordinal);
		if (!compact.Equals(providerDomain, StringComparison.OrdinalIgnoreCase))
			yield return compact;
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
