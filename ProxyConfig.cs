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

	[JsonPropertyName("logging")]
	public AppLoggingSettings Logging { get; set; } = new();

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

	[JsonPropertyName("openAIProvider")]
	public OpenAiGatewayProviderSettings OpenAIProvider { get; set; } = new();

	[JsonPropertyName("anthropicProvider")]
	public AnthropicGatewayProviderSettings AnthropicProvider { get; set; } = new();

	[JsonPropertyName("geminiProvider")]
	public GeminiGatewayProviderSettings GeminiProvider { get; set; } = new();

	[JsonPropertyName("xaiProvider")]
	public XAiGatewayProviderSettings XAiProvider { get; set; } = new();

	[JsonPropertyName("requestResponseLogging")]
	public LocalApiRequestResponseLoggingSettings RequestResponseLogging { get; set; } = new();

	[JsonPropertyName("includeErrorDiagnostics")]
	public bool IncludeErrorDiagnostics { get; set; } = true;
}

internal interface IGatewayProviderModel
{
	string Id { get; set; }
	bool Enabled { get; set; }
	string Protocol { get; set; }
	string Name { get; set; }
	string BaseUrl { get; set; }
	string DefaultModel { get; set; }
	string DefaultEmbeddingModel { get; set; }
	List<string> CachedModels { get; set; }
	List<string> CachedEmbeddingModels { get; set; }
	List<string> CachedMessageModels { get; set; }
	List<string> CachedOtherModels { get; set; }
	List<string> CachedModerationModels { get; set; }
	List<string> CachedUnknownModels { get; set; }
	Dictionary<string, string> CachedModelSummaries { get; set; }
	string AuthType { get; set; }
	string AuthHeaderName { get; set; }
	string ChatEndpoint { get; set; }
	string EmbeddingsEndpoint { get; set; }
	string ResponsesEndpoint { get; set; }
	List<LocalApiHeaderSetting> AdditionalHeaders { get; set; }
	List<GatewayRouteSettings> Routes { get; set; }
	GatewayProviderCapabilitySettings Capabilities { get; set; }
}

internal sealed class OpenAiGatewayProviderSettings : IGatewayProviderModel
{
	[JsonPropertyName("id")]
	public string Id { get; set; } = "openai";

	[JsonPropertyName("enabled")]
	public bool Enabled { get; set; } = true;

	[JsonPropertyName("protocol")]
	public string Protocol { get; set; } = "OpenAICompatible";

	[JsonPropertyName("name")]
	public string Name { get; set; } = "OpenAI";

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

	[JsonPropertyName("routes")]
	public List<GatewayRouteSettings> Routes { get; set; } = [];

	[JsonPropertyName("capabilities")]
	public GatewayProviderCapabilitySettings Capabilities { get; set; } = new();

	public OpenAiGatewayProviderSettings()
	{
		Capabilities = new GatewayProviderCapabilitySettings
		{
			SupportsChat = true,
			SupportsEmbeddings = true,
			SupportsResponses = true,
			SupportsStreaming = true
		};
	}
}

internal sealed class AnthropicGatewayProviderSettings : IGatewayProviderModel
{
	[JsonPropertyName("id")]
	public string Id { get; set; } = "anthropic";

	[JsonPropertyName("enabled")]
	public bool Enabled { get; set; } = true;

	[JsonPropertyName("protocol")]
	public string Protocol { get; set; } = "Anthropic";

	[JsonPropertyName("name")]
	public string Name { get; set; } = "Anthropic";

	[JsonPropertyName("baseUrl")]
	public string BaseUrl { get; set; } = "https://api.anthropic.com/v1/";

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
	public string AuthType { get; set; } = "Header";

	[JsonPropertyName("authHeaderName")]
	public string AuthHeaderName { get; set; } = "x-api-key";

	[JsonPropertyName("chatEndpoint")]
	public string ChatEndpoint { get; set; } = "messages";

	[JsonPropertyName("embeddingsEndpoint")]
	public string EmbeddingsEndpoint { get; set; } = string.Empty;

	[JsonPropertyName("responsesEndpoint")]
	public string ResponsesEndpoint { get; set; } = "responses";

	[JsonPropertyName("additionalHeaders")]
	public List<LocalApiHeaderSetting> AdditionalHeaders { get; set; } = [];

	[JsonPropertyName("routes")]
	public List<GatewayRouteSettings> Routes { get; set; } = [];

	[JsonPropertyName("capabilities")]
	public GatewayProviderCapabilitySettings Capabilities { get; set; } = new();

	public AnthropicGatewayProviderSettings()
	{
		Capabilities = new GatewayProviderCapabilitySettings
		{
			SupportsChat = true,
			SupportsEmbeddings = false,
			SupportsResponses = true,
			SupportsStreaming = true
		};
	}
}

internal sealed class GeminiGatewayProviderSettings : IGatewayProviderModel
{
	[JsonPropertyName("id")]
	public string Id { get; set; } = "google";

	[JsonPropertyName("enabled")]
	public bool Enabled { get; set; } = true;

	[JsonPropertyName("protocol")]
	public string Protocol { get; set; } = "OpenAICompatible";

	[JsonPropertyName("name")]
	public string Name { get; set; } = "Gemini";

	[JsonPropertyName("baseUrl")]
	public string BaseUrl { get; set; } = "https://generativelanguage.googleapis.com/v1beta/openai/";

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

	[JsonPropertyName("routes")]
	public List<GatewayRouteSettings> Routes { get; set; } = [];

	[JsonPropertyName("capabilities")]
	public GatewayProviderCapabilitySettings Capabilities { get; set; } = new();

	public GeminiGatewayProviderSettings()
	{
		Capabilities = new GatewayProviderCapabilitySettings
		{
			SupportsChat = true,
			SupportsEmbeddings = true,
			SupportsResponses = true,
			SupportsStreaming = true
		};
	}
}

internal sealed class XAiGatewayProviderSettings : IGatewayProviderModel
{
	[JsonPropertyName("id")]
	public string Id { get; set; } = "xai";

	[JsonPropertyName("enabled")]
	public bool Enabled { get; set; } = true;

	[JsonPropertyName("protocol")]
	public string Protocol { get; set; } = "OpenAICompatible";

	[JsonPropertyName("name")]
	public string Name { get; set; } = "xAI";

	[JsonPropertyName("baseUrl")]
	public string BaseUrl { get; set; } = "https://api.x.ai/v1/";

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
	public string EmbeddingsEndpoint { get; set; } = string.Empty;

	[JsonPropertyName("responsesEndpoint")]
	public string ResponsesEndpoint { get; set; } = "responses";

	[JsonPropertyName("additionalHeaders")]
	public List<LocalApiHeaderSetting> AdditionalHeaders { get; set; } = [];

	[JsonPropertyName("routes")]
	public List<GatewayRouteSettings> Routes { get; set; } = [];

	[JsonPropertyName("capabilities")]
	public GatewayProviderCapabilitySettings Capabilities { get; set; } = new();

	public XAiGatewayProviderSettings()
	{
		Capabilities = new GatewayProviderCapabilitySettings
		{
			SupportsChat = true,
			SupportsEmbeddings = false,
			SupportsResponses = true,
			SupportsStreaming = true
		};
	}
}

internal static class GatewayProviderModelHelpers
{
	public static string NormalizeProviderId(string? providerId)
	{
		if (string.IsNullOrWhiteSpace(providerId))
			return "openai";

		return providerId.Trim().ToLowerInvariant() switch
		{
			"gemini" => "google",
			"x.ai" => "xai",
			"x-ai" => "xai",
			"grok" => "xai",
			var value => value
		};
	}

	public static IEnumerable<IGatewayProviderModel> Enumerate(OllamaGatewaySettings settings)
	{
		ArgumentNullException.ThrowIfNull(settings);
		yield return settings.OpenAIProvider ?? new OpenAiGatewayProviderSettings();
		yield return settings.AnthropicProvider ?? new AnthropicGatewayProviderSettings();
		yield return settings.GeminiProvider ?? new GeminiGatewayProviderSettings();
		yield return settings.XAiProvider ?? new XAiGatewayProviderSettings();
	}

	public static IGatewayProviderModel? Find(OllamaGatewaySettings? settings, string? providerId)
	{
		if (settings is null)
			return null;

		return NormalizeProviderId(providerId) switch
		{
			"openai" => settings.OpenAIProvider,
			"anthropic" => settings.AnthropicProvider,
			"google" => settings.GeminiProvider,
			"xai" => settings.XAiProvider,
			_ => null
		};
	}

	public static IGatewayProviderModel GetDefault(OllamaGatewaySettings settings)
	{
		ArgumentNullException.ThrowIfNull(settings);
		return Enumerate(settings).FirstOrDefault(static provider => provider.Enabled) ?? settings.OpenAIProvider;
	}

	public static string InferProviderId(LocalApiProviderSettings? provider)
	{
		var protocol = provider?.Protocol ?? string.Empty;
		var name = provider?.Name ?? string.Empty;
		var baseUrl = provider?.BaseUrl ?? string.Empty;
		var combined = $"{protocol} {name} {baseUrl}";

		if (combined.Contains("anthropic", StringComparison.OrdinalIgnoreCase))
			return "anthropic";

		if (combined.Contains("gemini", StringComparison.OrdinalIgnoreCase)
			|| combined.Contains("google", StringComparison.OrdinalIgnoreCase)
			|| combined.Contains("generativelanguage.googleapis.com", StringComparison.OrdinalIgnoreCase))
		{
			return "google";
		}

		if (combined.Contains("x.ai", StringComparison.OrdinalIgnoreCase)
			|| combined.Contains("api.x.ai", StringComparison.OrdinalIgnoreCase)
			|| combined.Contains("xai", StringComparison.OrdinalIgnoreCase)
			|| combined.Contains("grok", StringComparison.OrdinalIgnoreCase))
		{
			return "xai";
		}

		return "openai";
	}

	public static void Normalize(OllamaGatewaySettings settings)
	{
		ArgumentNullException.ThrowIfNull(settings);
		settings.OpenAIProvider ??= new OpenAiGatewayProviderSettings();
		settings.AnthropicProvider ??= new AnthropicGatewayProviderSettings();
		settings.GeminiProvider ??= new GeminiGatewayProviderSettings();
		settings.XAiProvider ??= new XAiGatewayProviderSettings();
		NormalizeProvider(settings.OpenAIProvider, "openai");
		NormalizeProvider(settings.AnthropicProvider, "anthropic");
		NormalizeProvider(settings.GeminiProvider, "google");
		NormalizeProvider(settings.XAiProvider, "xai");
	}

	public static string GetPreferredModelSuffix(string? providerId)
	{
		return NormalizeProviderId(providerId) switch
		{
			"openai" => "openai",
			"anthropic" => "anthropic",
			"google" => "gemini",
			"xai" => "xai",
			_ => "upstream"
		};
	}

	public static IReadOnlyList<string> GetModelSuffixAliases(string? providerId)
	{
		return NormalizeProviderId(providerId) switch
		{
			"openai" => ["openai", "oai", "oa"],
			"anthropic" => ["anthropic", "claude", "ap"],
			"google" => ["gemini", "google", "gem", "ggl"],
			"xai" => ["xai", "grok", "x"],
			_ => []
		};
	}

	public static string? TryResolveProviderIdFromModelName(string? modelName)
	{
		if (string.IsNullOrWhiteSpace(modelName))
			return null;

		var trimmed = modelName.Trim();
		var suffixIndex = trimmed.LastIndexOf('@');
		if (suffixIndex <= 0 || suffixIndex >= trimmed.Length - 1)
			return null;

		var suffix = trimmed[(suffixIndex + 1)..].Trim();
		if (suffix.Length == 0)
			return null;

		foreach (var providerId in new[] { "openai", "anthropic", "google", "xai" })
		{
			if (GetModelSuffixAliases(providerId).Contains(suffix, StringComparer.OrdinalIgnoreCase))
				return providerId;
		}

		return null;
	}

	public static void ApplyLegacyProvider(OllamaGatewaySettings settings, LegacyGatewayProviderModel legacyProvider)
	{
		ArgumentNullException.ThrowIfNull(settings);
		ArgumentNullException.ThrowIfNull(legacyProvider);

		var provider = Find(settings, legacyProvider.Id);
		if (provider is null)
			return;

		provider.Id = NormalizeProviderId(legacyProvider.Id);
		provider.Enabled = legacyProvider.Enabled;
		provider.Protocol = legacyProvider.Protocol ?? provider.Protocol;
		provider.Name = legacyProvider.Name ?? provider.Name;
		provider.BaseUrl = legacyProvider.BaseUrl ?? provider.BaseUrl;
		provider.DefaultModel = legacyProvider.DefaultModel ?? string.Empty;
		provider.DefaultEmbeddingModel = legacyProvider.DefaultEmbeddingModel ?? string.Empty;
		provider.CachedModels = legacyProvider.CachedModels ?? [];
		provider.CachedEmbeddingModels = legacyProvider.CachedEmbeddingModels ?? [];
		provider.CachedMessageModels = legacyProvider.CachedMessageModels ?? [];
		provider.CachedOtherModels = legacyProvider.CachedOtherModels ?? [];
		provider.CachedModerationModels = legacyProvider.CachedModerationModels ?? [];
		provider.CachedUnknownModels = legacyProvider.CachedUnknownModels ?? [];
		provider.CachedModelSummaries = legacyProvider.CachedModelSummaries ?? new(StringComparer.OrdinalIgnoreCase);
		provider.AuthType = legacyProvider.AuthType ?? provider.AuthType;
		provider.AuthHeaderName = legacyProvider.AuthHeaderName ?? provider.AuthHeaderName;
		provider.ChatEndpoint = legacyProvider.ChatEndpoint ?? provider.ChatEndpoint;
		provider.EmbeddingsEndpoint = legacyProvider.EmbeddingsEndpoint ?? provider.EmbeddingsEndpoint;
		provider.ResponsesEndpoint = legacyProvider.ResponsesEndpoint ?? provider.ResponsesEndpoint;
		provider.AdditionalHeaders = legacyProvider.AdditionalHeaders ?? [];
		provider.Routes = legacyProvider.Routes ?? [];
		provider.Capabilities = legacyProvider.Capabilities ?? new GatewayProviderCapabilitySettings();
	}

	private static void NormalizeProvider(IGatewayProviderModel provider, string providerId)
	{
		provider.Id = providerId;
		provider.Protocol ??= "OpenAICompatible";
		provider.Name ??= providerId;
		provider.BaseUrl ??= string.Empty;
		provider.DefaultModel ??= string.Empty;
		provider.DefaultEmbeddingModel ??= string.Empty;
		provider.CachedModels ??= [];
		provider.CachedEmbeddingModels ??= [];
		provider.CachedMessageModels ??= [];
		provider.CachedOtherModels ??= [];
		provider.CachedModerationModels ??= [];
		provider.CachedUnknownModels ??= [];
		provider.CachedModelSummaries ??= new(StringComparer.OrdinalIgnoreCase);
		provider.AuthType ??= "Bearer";
		provider.AuthHeaderName ??= "Authorization";
		provider.ChatEndpoint ??= string.Empty;
		provider.EmbeddingsEndpoint ??= string.Empty;
		provider.ResponsesEndpoint ??= string.Empty;
		provider.AdditionalHeaders ??= [];
		provider.Routes ??= [];
		provider.Capabilities ??= new GatewayProviderCapabilitySettings();
	}
}

internal sealed class LegacyGatewayProviderModel
{
	public string Id { get; set; } = "openai";
	public bool Enabled { get; set; } = true;
	public string Protocol { get; set; } = "OpenAICompatible";
	public string Name { get; set; } = "OpenAI";
	public string BaseUrl { get; set; } = "https://api.openai.com/v1/";
	public string DefaultModel { get; set; } = string.Empty;
	public string DefaultEmbeddingModel { get; set; } = string.Empty;
	public List<string> CachedModels { get; set; } = [];
	public List<string> CachedEmbeddingModels { get; set; } = [];
	public List<string> CachedMessageModels { get; set; } = [];
	public List<string> CachedOtherModels { get; set; } = [];
	public List<string> CachedModerationModels { get; set; } = [];
	public List<string> CachedUnknownModels { get; set; } = [];
	public Dictionary<string, string> CachedModelSummaries { get; set; } = new(StringComparer.OrdinalIgnoreCase);
	public string AuthType { get; set; } = "Bearer";
	public string AuthHeaderName { get; set; } = "Authorization";
	public string ChatEndpoint { get; set; } = "chat/completions";
	public string EmbeddingsEndpoint { get; set; } = "embeddings";
	public string ResponsesEndpoint { get; set; } = "responses";
	public List<LocalApiHeaderSetting> AdditionalHeaders { get; set; } = [];
	public List<GatewayRouteSettings> Routes { get; set; } = [];
	public GatewayProviderCapabilitySettings Capabilities { get; set; } = new();
}

internal class GatewayRouteSettings
{
	[JsonPropertyName("localModel")]
	public string LocalModel { get; set; } = string.Empty;

	[JsonPropertyName("upstreamModel")]
	public string UpstreamModel { get; set; } = string.Empty;
}

internal sealed class LegacyGatewayRouteSettings
{
	[JsonPropertyName("localModel")]
	public string LocalModel { get; set; } = string.Empty;

	[JsonPropertyName("providerId")]
	public string ProviderId { get; set; } = "openai";

	[JsonPropertyName("upstreamModel")]
	public string UpstreamModel { get; set; } = string.Empty;
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
			var json = JsonSerializer.Serialize(CreatePersistedConfig(config), options);
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
			Logging = new AppLoggingSettings()
		};
	}

	private static OllamaGatewaySettings CreateDefaultGatewaySettings()
	{
		return new OllamaGatewaySettings
		{
			OpenAIProvider = new OpenAiGatewayProviderSettings(),
			AnthropicProvider = new AnthropicGatewayProviderSettings(),
			GeminiProvider = new GeminiGatewayProviderSettings(),
			XAiProvider = new XAiGatewayProviderSettings()
		};
	}

	private static bool TryNormalizeLoadedConfig(ProxyConfigModel config, string json)
	{
		ArgumentNullException.ThrowIfNull(config);

		var changed = config.OllamaGateway is null;
		if (config.Logging is null)
		{
			config.Logging = new AppLoggingSettings();
			changed = true;
		}
		if (config.ConfigSync is { Provider: { Length: > 0 } syncProvider } && !string.IsNullOrWhiteSpace(config.ConfigSync.GistId))
		{
			CredentialManager.SaveConfigSyncRemoteId(syncProvider, config.ConfigSync.GistId.Trim());
			config.ConfigSync.GistId = null;
			changed = true;
		}

		using var document = JsonDocument.Parse(json);
		var root = document.RootElement;

		config.OllamaGateway ??= CreateDefaultGatewaySettings();
		if (config.OllamaGateway is not null)
		{
			if (root.TryGetProperty("ollamaGateway", out var gatewayElement)
				&& gatewayElement.TryGetProperty("providers", out var providersElement)
				&& providersElement.ValueKind == JsonValueKind.Array)
			{
				foreach (var providerElement in providersElement.EnumerateArray())
				{
					var legacyProvider = providerElement.Deserialize<LegacyGatewayProviderModel>();
					if (legacyProvider is null)
						continue;

					GatewayProviderModelHelpers.ApplyLegacyProvider(config.OllamaGateway, legacyProvider);
					changed = true;
				}
			}

			if (root.TryGetProperty("ollamaGateway", out gatewayElement)
				&& gatewayElement.TryGetProperty("routes", out var routesElement)
				&& routesElement.ValueKind == JsonValueKind.Array)
			{
				foreach (var routeElement in routesElement.EnumerateArray())
				{
					var legacyRoute = routeElement.Deserialize<LegacyGatewayRouteSettings>();
					if (legacyRoute is null)
						continue;

					var provider = GatewayProviderModelHelpers.Find(config.OllamaGateway, legacyRoute.ProviderId);
					if (provider is null)
						continue;

					provider.Routes.Add(new GatewayRouteSettings
					{
						LocalModel = legacyRoute.LocalModel,
						UpstreamModel = legacyRoute.UpstreamModel
					});
					changed = true;
				}
			}

			NormalizeGatewaySettings(config.OllamaGateway);
			changed = true;
		}

		return changed;
	}

	private static object CreatePersistedConfig(ProxyConfigModel config)
	{
		return new
		{
			config.ConfigName,
			config.Proxy,
			config.Targeting,
			config.HostsRedirect,
			config.StartOnBoot,
			config.AutoStartProxy,
			configSync = CreatePersistedConfigSync(config.ConfigSync),
			ollamaGateway = CreatePersistedGateway(config.OllamaGateway),
			logging = config.Logging
		};
	}

	private static object? CreatePersistedConfigSync(ConfigSyncSettings? settings)
	{
		if (settings is null || string.IsNullOrWhiteSpace(settings.Provider))
			return null;

		return new
		{
			settings.Provider
		};
	}

	private static object? CreatePersistedGateway(OllamaGatewaySettings? settings)
	{
		if (settings is null)
			return null;

		return new
		{
			settings.Enabled,
			settings.OllamaPort,
			settings.RequestResponseLogging,
			settings.IncludeErrorDiagnostics,
			openAIProvider = CreatePersistedProvider(settings.OpenAIProvider),
			anthropicProvider = CreatePersistedProvider(settings.AnthropicProvider),
			geminiProvider = CreatePersistedProvider(settings.GeminiProvider),
			xaiProvider = CreatePersistedProvider(settings.XAiProvider)
		};
	}

	private static object CreatePersistedProvider(IGatewayProviderModel provider)
	{
		ArgumentNullException.ThrowIfNull(provider);

		return new
		{
			provider.Enabled,
			provider.BaseUrl
		};
	}

	internal static OllamaGatewaySettings BuildGatewaySettingsFromLegacy(LocalApiForwarderSettings? legacy)
	{
		legacy ??= new LocalApiForwarderSettings();

		var provider = legacy.Provider ?? new LocalApiProviderSettings();
		var providerId = GatewayProviderModelHelpers.InferProviderId(provider);
		var capabilities = new GatewayProviderCapabilitySettings
		{
			SupportsChat = true,
			SupportsResponses = true,
			SupportsStreaming = !string.Equals(provider.Protocol, "Anthropic", StringComparison.OrdinalIgnoreCase),
			SupportsEmbeddings = !string.Equals(provider.Protocol, "Anthropic", StringComparison.OrdinalIgnoreCase)
		};

		var settings = CreateDefaultGatewaySettings();
		settings.Enabled = legacy.Enabled;
		settings.OllamaPort = legacy.OllamaPort;
		settings.RequestResponseLogging = legacy.RequestResponseLogging ?? new LocalApiRequestResponseLoggingSettings();
		settings.IncludeErrorDiagnostics = legacy.IncludeErrorDiagnostics;

		settings.OpenAIProvider.Enabled = false;
		settings.AnthropicProvider.Enabled = false;
		settings.GeminiProvider.Enabled = false;
		settings.XAiProvider.Enabled = false;

		var targetProvider = GatewayProviderModelHelpers.Find(settings, providerId) ?? settings.OpenAIProvider;
		targetProvider.Id = providerId;
		targetProvider.Enabled = legacy.Enabled;
		targetProvider.Protocol = provider.Protocol;
		targetProvider.Name = provider.Name;
		targetProvider.BaseUrl = provider.BaseUrl;
		targetProvider.DefaultModel = provider.DefaultModel;
		targetProvider.DefaultEmbeddingModel = provider.DefaultEmbeddingModel;
		targetProvider.AuthType = provider.AuthType;
		targetProvider.AuthHeaderName = provider.AuthHeaderName;
		targetProvider.ChatEndpoint = provider.ChatEndpoint;
		targetProvider.EmbeddingsEndpoint = provider.EmbeddingsEndpoint;
		targetProvider.ResponsesEndpoint = provider.ResponsesEndpoint;
		targetProvider.AdditionalHeaders = provider.AdditionalHeaders ?? [];
		targetProvider.Capabilities = capabilities;
		targetProvider.Routes = legacy.ModelMappings
			.Select(mapping => new GatewayRouteSettings
			{
				LocalModel = mapping.LocalModel,
				UpstreamModel = mapping.UpstreamModel
			})
			.ToList();

		return settings;
	}

	private static void NormalizeGatewaySettings(OllamaGatewaySettings settings)
	{
		settings.RequestResponseLogging ??= new LocalApiRequestResponseLoggingSettings();
		GatewayProviderModelHelpers.Normalize(settings);
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
