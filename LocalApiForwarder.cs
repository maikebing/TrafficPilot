using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Globalization;

namespace TrafficPilot;

internal sealed record LocalApiAdvertisedModel(string LocalName, string UpstreamModel);

internal sealed class LocalApiForwarder : IDisposable
{
	private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
	private static readonly TimeSpan ModelCatalogSuccessCacheDuration = TimeSpan.FromMinutes(2);
	private static readonly TimeSpan ModelCatalogFailureCacheDuration = TimeSpan.FromSeconds(20);
	private const string OllamaCompatibilityVersion = "0.6.4";
	private const long DefaultContextLength = 100_000;
	private const long DefaultMaxOutputTokens = 8_192;

	private readonly LocalApiForwarderSettings _settings;
	private readonly string _apiKey;
	private readonly HttpClient _httpClient;
	private readonly CancellationTokenSource _cts = new();
	private readonly List<HttpListener> _listeners = [];
	private readonly List<Task> _acceptLoops = [];
	private readonly object _loadedModelsLock = new();
	private readonly SemaphoreSlim _modelCatalogSyncLock = new(1, 1);
	private readonly HashSet<string> _loadedModels = new(StringComparer.OrdinalIgnoreCase);
	private IReadOnlyList<ModelCatalogEntry> _cachedModelCatalog = [];
	private bool _hasCachedModelCatalog;
	private DateTimeOffset _modelCatalogExpiresAt = DateTimeOffset.MinValue;
	private long _requestCounter;
	private string? _providerModelAlias;

	public event Action<string>? OnLog;

	private sealed record ModelCatalogEntry(
		string LocalName,
		string UpstreamModel,
		string OwnedBy,
		long CreatedUnixTime,
		bool IsAlias,
		string Architecture,
		long ContextLength,
		long MaxOutputTokens,
		bool SupportsToolCalling,
		bool SupportsVision,
		bool SupportsEmbeddings,
		bool SupportsThinking,
		string ParameterSize,
		string QuantizationLevel,
		long? ParameterCount,
		double? Multiplier);

	private sealed class StreamingToolCallAccumulator
	{
		public string Id { get; set; } = string.Empty;
		public string Name { get; set; } = string.Empty;
		public StringBuilder Arguments { get; } = new();
	}

	public LocalApiForwarder(LocalApiForwarderSettings settings, string? apiKey)
	{
		_settings = settings ?? throw new ArgumentNullException(nameof(settings));
		_apiKey = apiKey?.Trim() ?? string.Empty;
		_httpClient = new HttpClient
		{
			Timeout = Timeout.InfiniteTimeSpan
		};

		foreach (var modelName in GetConfiguredLocalModelNames())
			_loadedModels.Add(modelName);
	}

	public Task StartAsync()
	{
		if (!_settings.Enabled)
			return Task.CompletedTask;

		ValidateSettings(_settings);
		LogStartupConfiguration();

		foreach (var port in GetDistinctPorts())
		{
			var listener = new HttpListener();
			listener.Prefixes.Add($"http://127.0.0.1:{port}/");
			listener.Prefixes.Add($"http://localhost:{port}/");
			listener.Start();
			_listeners.Add(listener);
			_acceptLoops.Add(AcceptLoopAsync(listener, _cts.Token));
			LogInfo($"Local API forwarder listening on http://127.0.0.1:{port}/");
		}

		return Task.CompletedTask;
	}

	public async Task<IReadOnlyList<string>> RefreshModelCatalogAsync(CancellationToken ct = default)
	{
		var models = await RefreshModelCatalogEntriesAsync(ct);
		return models.Select(static model => model.LocalName).ToList();
	}

	public async Task<IReadOnlyList<LocalApiAdvertisedModel>> RefreshModelCatalogEntriesAsync(CancellationToken ct = default)
	{
		if (!_settings.Enabled)
			throw new InvalidOperationException("Local API forwarding is disabled.");

		LogInfo("manual model catalog refresh requested");
		var catalog = await GetAdvertisedModelCatalogAsync(ct, forceRefresh: true);
		LogInfo($"manual model catalog refresh completed: {catalog.Count} models");
		return catalog
			.Select(static model => new LocalApiAdvertisedModel(model.LocalName, model.UpstreamModel))
			.ToList();
	}

	private void LogStartupConfiguration()
	{
		var provider = _settings.Provider ?? new LocalApiProviderSettings();
		LogInfo(
			$"startup config: provider='{provider.Name}', protocol={provider.Protocol}, baseUrl={provider.BaseUrl}, defaultModel={FormatSettingValue(provider.DefaultModel)}, embeddingModel={FormatSettingValue(provider.DefaultEmbeddingModel)}");
		LogInfo(
			$"startup ports: ollama={_settings.OllamaPort}, foundry={_settings.FoundryPort}, modelMappings={_settings.ModelMappings.Count}");
	}

	public async Task StopAsync()
	{
		_cts.Cancel();

		foreach (var listener in _listeners)
		{
			try { listener.Stop(); } catch { }
		}

		if (_acceptLoops.Count > 0)
			await Task.WhenAll(_acceptLoops);
	}

	public void Dispose()
	{
		_cts.Cancel();
		foreach (var listener in _listeners)
		{
			try { listener.Close(); } catch { }
		}

		_httpClient.Dispose();
		_modelCatalogSyncLock.Dispose();
		_cts.Dispose();
	}

	private static void ValidateSettings(LocalApiForwarderSettings settings)
	{
		if (settings.OllamaPort == 0)
			throw new InvalidOperationException("Ollama port must be greater than zero.");
		if (settings.FoundryPort == 0)
			throw new InvalidOperationException("Foundry port must be greater than zero.");
		if (settings.Provider is null)
			throw new InvalidOperationException("A third-party provider must be configured.");
		if (string.IsNullOrWhiteSpace(settings.Provider.BaseUrl))
			throw new InvalidOperationException("The third-party provider Base URL is required.");
		if (!Uri.TryCreate(settings.Provider.BaseUrl, UriKind.Absolute, out var baseUri)
			|| (baseUri.Scheme != Uri.UriSchemeHttp && baseUri.Scheme != Uri.UriSchemeHttps))
			throw new InvalidOperationException("The third-party provider Base URL must be an absolute HTTP or HTTPS address.");
	}

	private IEnumerable<ushort> GetDistinctPorts() =>
		new[] { _settings.OllamaPort, _settings.FoundryPort }.Distinct();

	private async Task AcceptLoopAsync(HttpListener listener, CancellationToken ct)
	{
		while (!ct.IsCancellationRequested)
		{
			HttpListenerContext? context = null;
			try
			{
				context = await listener.GetContextAsync();
				_ = HandleContextAsync(context, ct);
			}
			catch (HttpListenerException) when (ct.IsCancellationRequested || !listener.IsListening)
			{
				break;
			}
			catch (ObjectDisposedException) when (ct.IsCancellationRequested)
			{
				break;
			}
			catch (Exception ex)
			{
				LogInfo($"Local API accept error: {ex.Message}");
				if (context is not null)
				{
					try
					{
						context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
						context.Response.Close();
					}
					catch
					{
					}
				}
			}
		}
	}

	private async Task HandleContextAsync(HttpListenerContext context, CancellationToken ct)
	{
		var path = context.Request.Url?.AbsolutePath ?? "/";
		var requestId = Interlocked.Increment(ref _requestCounter);
		LogIncomingRequest(requestId, context.Request, path);
		try
		{
			if (HttpMethodsEqual(context.Request.HttpMethod, "GET") && path.Equals("/api/tags", StringComparison.OrdinalIgnoreCase))
			{
				await WriteJsonAsync(context.Response, HttpStatusCode.OK, await BuildOllamaTagsResponseAsync(ct));
				return;
			}

			if (HttpMethodsEqual(context.Request.HttpMethod, "GET") && path.Equals("/api/version", StringComparison.OrdinalIgnoreCase))
			{
				await WriteJsonAsync(context.Response, HttpStatusCode.OK, new JsonObject
				{
					// Some Ollama clients reject non-semver values here before attempting any requests.
					["version"] = OllamaCompatibilityVersion
				});
				return;
			}

			if (HttpMethodsEqual(context.Request.HttpMethod, "GET") && path.Equals("/api/ps", StringComparison.OrdinalIgnoreCase))
			{
				await WriteJsonAsync(context.Response, HttpStatusCode.OK, BuildOllamaPsResponse());
				return;
			}

			if (HttpMethodsEqual(context.Request.HttpMethod, "POST") && path.Equals("/api/show", StringComparison.OrdinalIgnoreCase))
			{
				await HandleOllamaShowAsync(context, ct);
				return;
			}

			if (HttpMethodsEqual(context.Request.HttpMethod, "GET") && path.Equals("/openai/status", StringComparison.OrdinalIgnoreCase))
			{
				await WriteJsonAsync(context.Response, HttpStatusCode.OK, BuildFoundryLocalStatusResponse());
				return;
			}

			if (HttpMethodsEqual(context.Request.HttpMethod, "GET") && path.Equals("/openai/models", StringComparison.OrdinalIgnoreCase))
			{
				await WriteJsonAsync(context.Response, HttpStatusCode.OK, await BuildFoundryLocalModelsResponseAsync(ct));
				return;
			}

			if (HttpMethodsEqual(context.Request.HttpMethod, "GET") && path.Equals("/openai/loadedmodels", StringComparison.OrdinalIgnoreCase))
			{
				await WriteJsonAsync(context.Response, HttpStatusCode.OK, BuildFoundryLocalModelsResponse(GetLoadedModelNames()));
				return;
			}

			if (HttpMethodsEqual(context.Request.HttpMethod, "GET") && path.Equals("/foundry/list", StringComparison.OrdinalIgnoreCase))
			{
				await WriteJsonAsync(context.Response, HttpStatusCode.OK, await BuildFoundryLocalCatalogResponseAsync(ct));
				return;
			}

			if (HttpMethodsEqual(context.Request.HttpMethod, "GET") && path.Equals("/openai/unloadall", StringComparison.OrdinalIgnoreCase))
			{
				ClearLoadedModels();
				await WriteEmptyAsync(context.Response, HttpStatusCode.OK);
				return;
			}

			if (HttpMethodsEqual(context.Request.HttpMethod, "GET") && TryExtractModelName(path, "/openai/load/", out var modelToLoad))
			{
				if (!await ModelExistsAsync(modelToLoad, ct))
				{
					await WriteErrorAsync(context.Response, path, HttpStatusCode.NotFound,
						$"Model '{modelToLoad}' is not registered in TrafficPilot.",
						"openai", null, null, modelToLoad);
					return;
				}

				MarkModelLoaded(modelToLoad);
				await WriteEmptyAsync(context.Response, HttpStatusCode.OK);
				return;
			}

			if (HttpMethodsEqual(context.Request.HttpMethod, "GET") && TryExtractModelName(path, "/openai/unload/", out var modelToUnload))
			{
				MarkModelUnloaded(modelToUnload);
				await WriteEmptyAsync(context.Response, HttpStatusCode.OK);
				return;
			}

			if (HttpMethodsEqual(context.Request.HttpMethod, "GET") && path.Equals("/openai/getgpudevice", StringComparison.OrdinalIgnoreCase))
			{
				await WritePlainTextAsync(context.Response, HttpStatusCode.OK, "0", "application/json");
				return;
			}

			if (HttpMethodsEqual(context.Request.HttpMethod, "GET") && TryExtractModelName(path, "/openai/setgpudevice/", out _))
			{
				await WriteEmptyAsync(context.Response, HttpStatusCode.OK);
				return;
			}

			if (HttpMethodsEqual(context.Request.HttpMethod, "GET")
				&& (path.Equals("/v1/models", StringComparison.OrdinalIgnoreCase)
					|| path.Equals("/models", StringComparison.OrdinalIgnoreCase)))
			{
				await WriteJsonAsync(context.Response, HttpStatusCode.OK, await BuildOpenAiModelsResponseAsync(ct));
				return;
			}

			if (HttpMethodsEqual(context.Request.HttpMethod, "POST") && path.Equals("/api/generate", StringComparison.OrdinalIgnoreCase))
			{
				await HandleOllamaGenerateAsync(context, ct);
				return;
			}

			if (HttpMethodsEqual(context.Request.HttpMethod, "POST") && path.Equals("/api/chat", StringComparison.OrdinalIgnoreCase))
			{
				await HandleOllamaChatAsync(context, ct);
				return;
			}

			if (HttpMethodsEqual(context.Request.HttpMethod, "POST")
				&& (path.Equals("/api/embeddings", StringComparison.OrdinalIgnoreCase)
					|| path.Equals("/api/embed", StringComparison.OrdinalIgnoreCase)))
			{
				await HandleOllamaEmbeddingsAsync(context, path, ct);
				return;
			}

			if (HttpMethodsEqual(context.Request.HttpMethod, "POST")
				&& (path.Equals("/v1/chat/completions", StringComparison.OrdinalIgnoreCase)
					|| path.Equals("/chat/completions", StringComparison.OrdinalIgnoreCase)))
			{
				await HandleOpenAiChatAsync(context, ct);
				return;
			}

			if (HttpMethodsEqual(context.Request.HttpMethod, "POST")
				&& (path.Equals("/v1/embeddings", StringComparison.OrdinalIgnoreCase)
					|| path.Equals("/embeddings", StringComparison.OrdinalIgnoreCase)))
			{
				await HandleOpenAiEmbeddingsAsync(context, ct);
				return;
			}

			if (HttpMethodsEqual(context.Request.HttpMethod, "POST")
				&& (path.Equals("/v1/responses", StringComparison.OrdinalIgnoreCase)
					|| path.Equals("/responses", StringComparison.OrdinalIgnoreCase)))
			{
				await HandleOpenAiResponsesAsync(context, ct);
				return;
			}

			await WriteErrorAsync(context.Response, path, HttpStatusCode.NotFound, $"Unsupported local API path: {path}", "openai", null, null, null);
		}
		catch (Exception ex) when (ex is not OperationCanceledException)
		{
			LogInfo($"request #{requestId} failed on {path}: {ex.Message}");
			var style = path.StartsWith("/api/", StringComparison.OrdinalIgnoreCase) ? "ollama" : "openai";
			await WriteErrorAsync(context.Response, path, HttpStatusCode.BadGateway, ex.Message, style, null, null, null);
		}
	}

	private async Task HandleOpenAiChatAsync(HttpListenerContext context, CancellationToken ct)
	{
		var requestJson = await ReadRequestJsonAsync(context.Request, ct);
		var requestedModel = requestJson["model"]?.GetValue<string>() ?? string.Empty;
		var stream = requestJson["stream"]?.GetValue<bool>() ?? false;
		LogRequestIfEnabled("openai.chat", requestJson);

		if (IsAnthropicProvider)
		{
			if (stream)
			{
				await WriteErrorAsync(context.Response, "/v1/chat/completions", HttpStatusCode.BadRequest,
					"Streaming passthrough is not supported in the Anthropic adapter yet.",
					"openai", null, null, null);
				return;
			}

			var anthropicRequest = BuildAnthropicMessagesRequestFromOpenAi(requestJson);
			using var upstreamRequest = CreateUpstreamRequest(_settings.Provider.ChatEndpoint, anthropicRequest);
			using var upstreamResponse = await _httpClient.SendAsync(upstreamRequest, HttpCompletionOption.ResponseHeadersRead, ct);
			var body = await upstreamResponse.Content.ReadAsStringAsync(ct);
			LogResponseIfEnabled("openai.chat", upstreamResponse.StatusCode, body);

			if (!upstreamResponse.IsSuccessStatusCode)
			{
				await WriteErrorAsync(context.Response, "/v1/chat/completions", upstreamResponse.StatusCode,
					await ExtractUpstreamErrorAsync(upstreamResponse.StatusCode, body),
					"openai", upstreamResponse.StatusCode, body, null);
				return;
			}

			var upstreamJson = ParseJsonObject(body, "Anthropic response");
			var openAiResponse = ConvertAnthropicResponseToOpenAiChat(upstreamJson, requestedModel);
			await WriteJsonAsync(context.Response, HttpStatusCode.OK, openAiResponse);
			return;
		}

		requestJson["model"] = ResolveUpstreamChatModel(requestedModel);
		using var request = CreateUpstreamRequest(_settings.Provider.ChatEndpoint, requestJson);
		using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);

		if (stream)
		{
			LogInfo($"Forwarded OpenAI-compatible streaming chat request '{requestedModel}' to {_settings.Provider.BaseUrl}");
			await CopyUpstreamResponseAsync(context.Response, response, passthroughStreaming: true, ct);
			return;
		}

		var responseBody = await response.Content.ReadAsStringAsync(ct);
		LogResponseIfEnabled("openai.chat", response.StatusCode, responseBody);
		if (!response.IsSuccessStatusCode)
		{
			await WriteErrorAsync(context.Response, "/v1/chat/completions", response.StatusCode,
				await ExtractUpstreamErrorAsync(response.StatusCode, responseBody),
				"openai", response.StatusCode, responseBody, null);
			return;
		}

		await WriteJsonAsync(context.Response, response.StatusCode, responseBody);
	}

	private async Task HandleOpenAiEmbeddingsAsync(HttpListenerContext context, CancellationToken ct)
	{
		var requestJson = await ReadRequestJsonAsync(context.Request, ct);
		LogRequestIfEnabled("openai.embeddings", requestJson);
		var requestedModel = requestJson["model"]?.GetValue<string>() ?? string.Empty;

		if (IsAnthropicProvider)
		{
			await WriteErrorAsync(context.Response, "/v1/embeddings", HttpStatusCode.BadRequest,
				"The Anthropic adapter does not provide an embeddings endpoint.",
				"openai", null, null, null);
			return;
		}

		requestJson["model"] = ResolveUpstreamEmbeddingsModel(requestedModel);
		using var request = CreateUpstreamRequest(_settings.Provider.EmbeddingsEndpoint, requestJson);
		using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
		var responseBody = await response.Content.ReadAsStringAsync(ct);
		LogResponseIfEnabled("openai.embeddings", response.StatusCode, responseBody);

		if (!response.IsSuccessStatusCode)
		{
			await WriteErrorAsync(context.Response, "/v1/embeddings", response.StatusCode,
				await ExtractUpstreamErrorAsync(response.StatusCode, responseBody),
				"openai", response.StatusCode, responseBody, null);
			return;
		}

		await WriteJsonAsync(context.Response, response.StatusCode, responseBody);
	}

	private async Task HandleOpenAiResponsesAsync(HttpListenerContext context, CancellationToken ct)
	{
		var requestJson = await ReadRequestJsonAsync(context.Request, ct);
		LogRequestIfEnabled("openai.responses", requestJson);
		var requestedModel = requestJson["model"]?.GetValue<string>() ?? string.Empty;
		var stream = requestJson["stream"]?.GetValue<bool>() ?? false;

		if (IsAnthropicProvider)
		{
			if (stream)
			{
				await WriteErrorAsync(context.Response, "/v1/responses", HttpStatusCode.BadRequest,
					"Streaming responses are not supported in the Anthropic adapter yet.",
					"openai", null, null, null);
				return;
			}

			var anthropicRequest = BuildAnthropicMessagesRequestFromResponses(requestJson);
			using var upstreamRequest = CreateUpstreamRequest(_settings.Provider.ChatEndpoint, anthropicRequest);
			using var upstreamResponse = await _httpClient.SendAsync(upstreamRequest, HttpCompletionOption.ResponseHeadersRead, ct);
			var body = await upstreamResponse.Content.ReadAsStringAsync(ct);
			LogResponseIfEnabled("openai.responses", upstreamResponse.StatusCode, body);

			if (!upstreamResponse.IsSuccessStatusCode)
			{
				await WriteErrorAsync(context.Response, "/v1/responses", upstreamResponse.StatusCode,
					await ExtractUpstreamErrorAsync(upstreamResponse.StatusCode, body),
					"openai", upstreamResponse.StatusCode, body, null);
				return;
			}

			var upstreamJson = ParseJsonObject(body, "Anthropic response");
			var responseJson = ConvertAnthropicResponseToResponses(upstreamJson, requestedModel);
			await WriteJsonAsync(context.Response, HttpStatusCode.OK, responseJson);
			return;
		}

		// For OpenAI-compatible providers, passthrough directly to the upstream Responses API.
		requestJson["model"] = ResolveUpstreamChatModel(requestedModel);
		using var request = CreateUpstreamRequest(_settings.Provider.ResponsesEndpoint, requestJson);
		using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);

		if (stream)
		{
			LogInfo($"Forwarded Responses API streaming request '{requestedModel}' to {_settings.Provider.BaseUrl}");
			await CopyUpstreamResponseAsync(context.Response, response, passthroughStreaming: true, ct);
			return;
		}

		var responseBody = await response.Content.ReadAsStringAsync(ct);
		LogResponseIfEnabled("openai.responses", response.StatusCode, responseBody);

		if (!response.IsSuccessStatusCode)
		{
			await WriteErrorAsync(context.Response, "/v1/responses", response.StatusCode,
				await ExtractUpstreamErrorAsync(response.StatusCode, responseBody),
				"openai", response.StatusCode, responseBody, null);
			return;
		}

		await WriteJsonAsync(context.Response, response.StatusCode, responseBody);
	}

	private async Task HandleOllamaGenerateAsync(HttpListenerContext context, CancellationToken ct)
	{
		var requestJson = await ReadRequestJsonAsync(context.Request, ct);
		var localModel = requestJson["model"]?.GetValue<string>() ?? string.Empty;
		var stream = requestJson["stream"]?.GetValue<bool>() ?? false;
		LogRequestIfEnabled("ollama.generate", requestJson);

		if (IsAnthropicProvider)
		{
			if (stream)
			{
				await WriteErrorAsync(context.Response, "/api/generate", HttpStatusCode.BadRequest,
					"Streaming passthrough is not supported in the Anthropic adapter yet.",
					"ollama", null, null, localModel);
				return;
			}

			var anthropicRequest = BuildAnthropicMessagesRequestFromGenerate(requestJson);
			using var upstreamRequest = CreateUpstreamRequest(_settings.Provider.ChatEndpoint, anthropicRequest);
			using var upstreamResponse = await _httpClient.SendAsync(upstreamRequest, HttpCompletionOption.ResponseHeadersRead, ct);
			var body = await upstreamResponse.Content.ReadAsStringAsync(ct);
			LogResponseIfEnabled("ollama.generate", upstreamResponse.StatusCode, body);

			if (!upstreamResponse.IsSuccessStatusCode)
			{
				await WriteErrorAsync(context.Response, "/api/generate", upstreamResponse.StatusCode,
					await ExtractUpstreamErrorAsync(upstreamResponse.StatusCode, body),
					"ollama", upstreamResponse.StatusCode, body, localModel);
				return;
			}

			var upstreamJson = ParseJsonObject(body, "Anthropic response");
			var ollamaResponse = ConvertAnthropicResponseToOllama(upstreamJson, localModel, isChat: false);
			await WriteJsonAsync(context.Response, HttpStatusCode.OK, ollamaResponse);
			return;
		}

		var upstreamRequestJson = BuildUpstreamChatRequestFromGenerate(requestJson);
		using var upstreamRequest2 = CreateUpstreamRequest(_settings.Provider.ChatEndpoint, upstreamRequestJson);
		using var upstreamResponse2 = await _httpClient.SendAsync(upstreamRequest2, HttpCompletionOption.ResponseHeadersRead, ct);

		LogInfo($"Forwarded Ollama generate request '{localModel}' to {_settings.Provider.BaseUrl}");
		if (stream)
		{
			await WriteOllamaStreamingResponseAsync(
				context.Response,
				upstreamResponse2,
				string.IsNullOrWhiteSpace(localModel) ? ResolveUpstreamChatModel(string.Empty) : localModel,
				isChat: false,
				ct);
			return;
		}

		var body2 = await upstreamResponse2.Content.ReadAsStringAsync(ct);
		LogResponseIfEnabled("ollama.generate", upstreamResponse2.StatusCode, body2);
		if (!upstreamResponse2.IsSuccessStatusCode)
		{
			await WriteErrorAsync(context.Response, "/api/generate", upstreamResponse2.StatusCode,
				await ExtractUpstreamErrorAsync(upstreamResponse2.StatusCode, body2),
				"ollama", upstreamResponse2.StatusCode, body2, localModel);
			return;
		}

		var parsed = ParseJsonObject(body2, "OpenAI chat response");
		await WriteJsonAsync(
			context.Response,
			HttpStatusCode.OK,
			ConvertOpenAiChatResponseToOllama(parsed, localModel, isChat: false),
			contentType: "application/json");
	}

	private async Task HandleOllamaChatAsync(HttpListenerContext context, CancellationToken ct)
	{
		var requestJson = await ReadRequestJsonAsync(context.Request, ct);
		var localModel = requestJson["model"]?.GetValue<string>() ?? string.Empty;
		var stream = requestJson["stream"]?.GetValue<bool>() ?? false;
		LogRequestIfEnabled("ollama.chat", requestJson);

		if (IsAnthropicProvider)
		{
			if (stream)
			{
				await WriteErrorAsync(context.Response, "/api/chat", HttpStatusCode.BadRequest,
					"Streaming passthrough is not supported in the Anthropic adapter yet.",
					"ollama", null, null, localModel);
				return;
			}

			var anthropicRequest = BuildAnthropicMessagesRequestFromOllamaChat(requestJson);
			using var upstreamRequest = CreateUpstreamRequest(_settings.Provider.ChatEndpoint, anthropicRequest);
			using var upstreamResponse = await _httpClient.SendAsync(upstreamRequest, HttpCompletionOption.ResponseHeadersRead, ct);
			var body = await upstreamResponse.Content.ReadAsStringAsync(ct);
			LogResponseIfEnabled("ollama.chat", upstreamResponse.StatusCode, body);

			if (!upstreamResponse.IsSuccessStatusCode)
			{
				await WriteErrorAsync(context.Response, "/api/chat", upstreamResponse.StatusCode,
					await ExtractUpstreamErrorAsync(upstreamResponse.StatusCode, body),
					"ollama", upstreamResponse.StatusCode, body, localModel);
				return;
			}

			var upstreamJson = ParseJsonObject(body, "Anthropic response");
			var ollamaResponse = ConvertAnthropicResponseToOllama(upstreamJson, localModel, isChat: true);
			await WriteJsonAsync(context.Response, HttpStatusCode.OK, ollamaResponse);
			return;
		}

		var upstreamRequestJson = BuildUpstreamChatRequestFromChat(requestJson);
		using var upstreamRequest2 = CreateUpstreamRequest(_settings.Provider.ChatEndpoint, upstreamRequestJson);
		using var upstreamResponse2 = await _httpClient.SendAsync(upstreamRequest2, HttpCompletionOption.ResponseHeadersRead, ct);

		LogInfo($"Forwarded Ollama chat request '{localModel}' to {_settings.Provider.BaseUrl}");
		if (stream)
		{
			await WriteOllamaStreamingResponseAsync(
				context.Response,
				upstreamResponse2,
				string.IsNullOrWhiteSpace(localModel) ? ResolveUpstreamChatModel(string.Empty) : localModel,
				isChat: true,
				ct);
			return;
		}

		var body2 = await upstreamResponse2.Content.ReadAsStringAsync(ct);
		LogResponseIfEnabled("ollama.chat", upstreamResponse2.StatusCode, body2);
		if (!upstreamResponse2.IsSuccessStatusCode)
		{
			await WriteErrorAsync(context.Response, "/api/chat", upstreamResponse2.StatusCode,
				await ExtractUpstreamErrorAsync(upstreamResponse2.StatusCode, body2),
				"ollama", upstreamResponse2.StatusCode, body2, localModel);
			return;
		}

		var parsed = ParseJsonObject(body2, "OpenAI chat response");
		await WriteJsonAsync(
			context.Response,
			HttpStatusCode.OK,
			ConvertOpenAiChatResponseToOllama(parsed, localModel, isChat: true),
			contentType: "application/json");
	}

	private async Task HandleOllamaEmbeddingsAsync(HttpListenerContext context, string path, CancellationToken ct)
	{
		var requestJson = await ReadRequestJsonAsync(context.Request, ct);
		var localModel = requestJson["model"]?.GetValue<string>() ?? string.Empty;
		LogRequestIfEnabled("ollama.embeddings", requestJson);

		if (IsAnthropicProvider)
		{
			await WriteErrorAsync(context.Response, path, HttpStatusCode.BadRequest,
				"The Anthropic adapter does not provide an embeddings endpoint.",
				"ollama", null, null, localModel);
			return;
		}

		var upstreamRequestJson = BuildUpstreamEmbeddingsRequestFromOllama(requestJson);
		using var request = CreateUpstreamRequest(_settings.Provider.EmbeddingsEndpoint, upstreamRequestJson);
		using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
		var responseBody = await response.Content.ReadAsStringAsync(ct);
		LogResponseIfEnabled("ollama.embeddings", response.StatusCode, responseBody);

		if (!response.IsSuccessStatusCode)
		{
			await WriteErrorAsync(context.Response, path, response.StatusCode,
				await ExtractUpstreamErrorAsync(response.StatusCode, responseBody),
				"ollama", response.StatusCode, responseBody, localModel);
			return;
		}

		var parsed = ParseJsonObject(responseBody, "OpenAI embeddings response");
		var converted = ConvertOpenAiEmbeddingsResponseToOllama(parsed, localModel, path);
		await WriteJsonAsync(context.Response, HttpStatusCode.OK, converted);
	}

	private async Task HandleOllamaShowAsync(HttpListenerContext context, CancellationToken ct)
	{
		var requestJson = await ReadRequestJsonAsync(context.Request, ct);
		var modelName = requestJson["model"]?.GetValue<string>()
			?? requestJson["name"]?.GetValue<string>()
			?? string.Empty;

		if (string.IsNullOrWhiteSpace(modelName))
		{
			await WriteErrorAsync(context.Response, "/api/show", HttpStatusCode.BadRequest,
				"Ollama show requests must include a model name.",
				"ollama", null, null, null);
			return;
		}

		if (!await ModelExistsAsync(modelName, ct))
		{
			await WriteErrorAsync(context.Response, "/api/show", HttpStatusCode.NotFound,
				$"Model '{modelName}' is not registered in TrafficPilot.",
				"ollama", null, null, modelName);
			return;
		}

		await WriteJsonAsync(context.Response, HttpStatusCode.OK, await BuildOllamaShowResponseAsync(modelName, ct));
	}

	private HttpRequestMessage CreateUpstreamRequest(string relativePath, JsonNode payload)
	{
		var requestUri = BuildUpstreamUri(relativePath);
		var requestBytes = Encoding.UTF8.GetBytes(payload.ToJsonString(JsonOptions));
		var content = new ByteArrayContent(requestBytes);
		content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");
		var request = new HttpRequestMessage(HttpMethod.Post, requestUri)
		{
			Content = content
		};

		request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
		LogInfo($"upstream POST {requestUri} | content-type=application/json | bytes={requestBytes.Length}");
		ApplyAuthentication(request);
		foreach (var header in _settings.Provider.AdditionalHeaders)
		{
			if (string.IsNullOrWhiteSpace(header.Name))
				continue;

			request.Headers.TryAddWithoutValidation(header.Name.Trim(), header.Value ?? string.Empty);
		}

		return request;
	}

	private HttpRequestMessage CreateUpstreamGetRequest(string relativePath)
	{
		var requestUri = BuildUpstreamUri(relativePath);
		return CreateUpstreamGetRequest(requestUri);
	}

	private HttpRequestMessage CreateUpstreamGetRequest(Uri requestUri)
	{
		var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
		request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
		LogInfo($"upstream GET {requestUri} | accept=application/json");
		ApplyAuthentication(request);
		foreach (var header in _settings.Provider.AdditionalHeaders)
		{
			if (string.IsNullOrWhiteSpace(header.Name))
				continue;

			request.Headers.TryAddWithoutValidation(header.Name.Trim(), header.Value ?? string.Empty);
		}

		return request;
	}

	private Uri BuildUpstreamUri(string relativePath)
	{
		var baseUri = NormalizeBaseUri(_settings.Provider.BaseUrl);
		var relative = (relativePath ?? string.Empty).TrimStart('/');
		var uri = new Uri(baseUri, relative);
		return AppendQueryAuthIfNeeded(uri);
	}

	private Uri BuildUpstreamRootUri(string relativePath)
	{
		var baseUri = NormalizeBaseUri(_settings.Provider.BaseUrl);
		var rootUri = new Uri(baseUri.GetLeftPart(UriPartial.Authority) + "/", UriKind.Absolute);
		var relative = (relativePath ?? string.Empty).TrimStart('/');
		var uri = new Uri(rootUri, relative);
		return AppendQueryAuthIfNeeded(uri);
	}

	private Uri AppendQueryAuthIfNeeded(Uri uri)
	{

		if ((_settings.Provider.AuthType ?? "Bearer").Equals("Query", StringComparison.OrdinalIgnoreCase)
			&& !string.IsNullOrWhiteSpace(_apiKey))
		{
			var keyName = string.IsNullOrWhiteSpace(_settings.Provider.AuthHeaderName)
				? "key"
				: _settings.Provider.AuthHeaderName.Trim();
			var separator = string.IsNullOrEmpty(uri.Query) ? "?" : "&";
			return new Uri($"{uri}{separator}{Uri.EscapeDataString(keyName)}={Uri.EscapeDataString(_apiKey)}", UriKind.Absolute);
		}

		return uri;
	}

	private void ApplyAuthentication(HttpRequestMessage request)
	{
		if (string.IsNullOrWhiteSpace(_apiKey))
			return;

		var authType = (_settings.Provider.AuthType ?? "Bearer").Trim();
		if (authType.Equals("Bearer", StringComparison.OrdinalIgnoreCase))
		{
			request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
			return;
		}

		if (authType.Equals("Header", StringComparison.OrdinalIgnoreCase))
		{
			var headerName = string.IsNullOrWhiteSpace(_settings.Provider.AuthHeaderName)
				? "x-api-key"
				: _settings.Provider.AuthHeaderName.Trim();
			request.Headers.TryAddWithoutValidation(headerName, _apiKey);
		}
	}

	private static Uri NormalizeBaseUri(string baseUrl)
	{
		if (baseUrl.EndsWith("/", StringComparison.Ordinal))
			return new Uri(baseUrl, UriKind.Absolute);

		return new Uri($"{baseUrl}/", UriKind.Absolute);
	}

	private bool IsAnthropicProvider =>
		(_settings.Provider.Protocol ?? "OpenAICompatible").Equals("Anthropic", StringComparison.OrdinalIgnoreCase);

	private string ProviderModelAlias =>
		_providerModelAlias ??= BuildProviderModelAlias(_settings.Provider.Name, _settings.Provider.BaseUrl);

	private string ResolveUpstreamChatModel(string? localModel)
	{
		if (!string.IsNullOrWhiteSpace(localModel))
		{
			var mapping = _settings.ModelMappings.FirstOrDefault(m =>
				m.LocalModel.Equals(localModel, StringComparison.OrdinalIgnoreCase)
				&& !string.IsNullOrWhiteSpace(m.UpstreamModel));
			if (mapping is not null)
				return mapping.UpstreamModel.Trim();

			return ResolveGeneratedUpstreamModelAliasOrSelf(localModel);
		}

		if (!string.IsNullOrWhiteSpace(_settings.Provider.DefaultModel))
			return ResolveGeneratedUpstreamModelAliasOrSelf(_settings.Provider.DefaultModel);

		return string.Empty;
	}

	private string ResolveUpstreamEmbeddingsModel(string? localModel)
	{
		if (!string.IsNullOrWhiteSpace(localModel))
		{
			var mapping = _settings.ModelMappings.FirstOrDefault(m =>
				m.LocalModel.Equals(localModel, StringComparison.OrdinalIgnoreCase)
				&& !string.IsNullOrWhiteSpace(m.UpstreamModel));
			if (mapping is not null)
				return mapping.UpstreamModel.Trim();

			return ResolveGeneratedUpstreamModelAliasOrSelf(localModel);
		}

		if (!string.IsNullOrWhiteSpace(_settings.Provider.DefaultEmbeddingModel))
			return ResolveGeneratedUpstreamModelAliasOrSelf(_settings.Provider.DefaultEmbeddingModel);

		return ResolveUpstreamChatModel(string.Empty);
	}

	private string ResolveGeneratedUpstreamModelAliasOrSelf(string? modelName)
	{
		if (string.IsNullOrWhiteSpace(modelName))
			return string.Empty;

		var normalizedModel = modelName.Trim();
		var catalogMatch = TryResolveCatalogMappedUpstreamModel(normalizedModel);
		if (!string.IsNullOrWhiteSpace(catalogMatch))
			return catalogMatch;

		foreach (var alias in GetProviderModelAliasCandidates(_settings.Provider.Name, _settings.Provider.BaseUrl))
		{
			var aliasSuffix = $"@{alias}";
			if (!normalizedModel.EndsWith(aliasSuffix, StringComparison.OrdinalIgnoreCase))
				continue;

			var upstreamModel = normalizedModel[..^aliasSuffix.Length].Trim();
			if (!string.IsNullOrWhiteSpace(upstreamModel))
				return upstreamModel;
		}

		return normalizedModel;
	}

	private string BuildAdvertisedUpstreamLocalName(string upstreamModel)
	{
		return BuildAdvertisedUpstreamLocalName(upstreamModel, null);
	}

	private string BuildAdvertisedUpstreamLocalName(string upstreamModel, string? displayName)
	{
		var normalizedModel = ResolveGeneratedUpstreamModelAliasOrSelf(upstreamModel);
		if (string.IsNullOrWhiteSpace(normalizedModel))
			return string.Empty;

		var preferredName = string.IsNullOrWhiteSpace(displayName)
			? normalizedModel
			: displayName.Trim();

		return $"{preferredName}@{ProviderModelAlias}";
	}

	private JsonObject BuildUpstreamChatRequestFromGenerate(JsonObject source)
	{
		var localModel = source["model"]?.GetValue<string>() ?? string.Empty;
		var messages = new JsonArray();
		var system = source["system"]?.GetValue<string>();
		if (!string.IsNullOrWhiteSpace(system))
		{
			messages.Add(new JsonObject
			{
				["role"] = "system",
				["content"] = system
			});
		}

		var prompt = source["prompt"]?.GetValue<string>();
		if (!string.IsNullOrWhiteSpace(prompt))
		{
			messages.Add(new JsonObject
			{
				["role"] = "user",
				["content"] = prompt
			});
		}

		if (messages.Count == 0)
			throw new InvalidOperationException("Ollama generate requests must include a prompt.");

		var upstream = new JsonObject
		{
			["model"] = ResolveUpstreamChatModel(localModel),
			["messages"] = messages,
			["stream"] = source["stream"]?.GetValue<bool>() ?? false
		};

		CopyCommonGenerationOptions(source, upstream);
		return upstream;
	}

	private JsonObject BuildUpstreamChatRequestFromChat(JsonObject source)
	{
		var localModel = source["model"]?.GetValue<string>() ?? string.Empty;
		if (source["messages"] is not JsonArray messages || messages.Count == 0)
			throw new InvalidOperationException("Ollama chat requests must include messages.");

		var upstream = new JsonObject
		{
			["model"] = ResolveUpstreamChatModel(localModel),
			["messages"] = ConvertOllamaMessagesToOpenAiMessages(messages),
			["stream"] = source["stream"]?.GetValue<bool>() ?? false
		};

		CopyCommonGenerationOptions(source, upstream);
		return upstream;
	}

	private JsonArray ConvertOllamaMessagesToOpenAiMessages(JsonArray sourceMessages)
	{
		var converted = new JsonArray();
		var pendingToolCallIdsByName = new Dictionary<string, Queue<string>>(StringComparer.OrdinalIgnoreCase);

		foreach (var message in sourceMessages.OfType<JsonObject>())
		{
			var role = message["role"]?.GetValue<string>() ?? "user";
			if (role.Equals("tool", StringComparison.OrdinalIgnoreCase))
			{
				converted.Add(ConvertOllamaToolMessageToOpenAi(message, pendingToolCallIdsByName));
				continue;
			}

			var openAiMessage = new JsonObject
			{
				["role"] = role
			};

			openAiMessage["content"] = ConvertOllamaMessageContentToOpenAi(message);
			if (message["tool_calls"] is JsonArray toolCalls && toolCalls.Count > 0)
				openAiMessage["tool_calls"] = ConvertOllamaToolCallsToOpenAi(toolCalls, pendingToolCallIdsByName);

			converted.Add(openAiMessage);
		}

		return converted;
	}

	private JsonNode ConvertOllamaMessageContentToOpenAi(JsonObject message)
	{
		if (message["content"] is JsonArray contentArray && (message["images"] as JsonArray)?.Count is not > 0)
			return contentArray.DeepClone();

		if (message["images"] is not JsonArray images || images.Count == 0)
			return message["content"]?.DeepClone() ?? string.Empty;

		var content = new JsonArray();
		var text = ExtractTextContent(message["content"]);
		if (!string.IsNullOrWhiteSpace(text))
		{
			content.Add(new JsonObject
			{
				["type"] = "text",
				["text"] = text
			});
		}

		foreach (var imageNode in images)
		{
			var image = TryReadString(imageNode);
			if (string.IsNullOrWhiteSpace(image))
				continue;

			content.Add(new JsonObject
			{
				["type"] = "image_url",
				["image_url"] = new JsonObject
				{
					["url"] = NormalizeOllamaImageReference(image)
				}
			});
		}

		return content.Count == 0 ? string.Empty : content;
	}

	private static JsonArray ConvertOllamaToolCallsToOpenAi(
		JsonArray toolCalls,
		IDictionary<string, Queue<string>> pendingToolCallIdsByName)
	{
		var converted = new JsonArray();
		var position = 0;
		foreach (var toolCall in toolCalls.OfType<JsonObject>())
		{
			var function = toolCall["function"] as JsonObject;
			if (function is null)
				continue;

			var toolName = function["name"]?.GetValue<string>() ?? "tool";
			var index = TryReadInt64(function["index"]) ?? position;
			var toolCallId = TryReadString(toolCall["id"]) ?? $"call_{index}_{BuildArchitectureKey(toolName)}";
			var argumentsNode = function["arguments"]?.DeepClone() ?? new JsonObject();
			var argumentsJson = argumentsNode is JsonValue value && value.TryGetValue<string>(out var rawArguments)
				? rawArguments
				: argumentsNode.ToJsonString(JsonOptions);

			if (!pendingToolCallIdsByName.TryGetValue(toolName, out var queue))
			{
				queue = new Queue<string>();
				pendingToolCallIdsByName[toolName] = queue;
			}

			queue.Enqueue(toolCallId);
			converted.Add(new JsonObject
			{
				["id"] = toolCallId,
				["type"] = "function",
				["function"] = new JsonObject
				{
					["name"] = toolName,
					["arguments"] = argumentsJson
				}
			});
			position++;
		}

		return converted;
	}

	private static JsonObject ConvertOllamaToolMessageToOpenAi(
		JsonObject message,
		IDictionary<string, Queue<string>> pendingToolCallIdsByName)
	{
		var toolName = message["tool_name"]?.GetValue<string>()
			?? message["name"]?.GetValue<string>()
			?? "tool";
		var toolCallId = TryReadString(message["tool_call_id"])
			?? ResolvePendingToolCallId(toolName, pendingToolCallIdsByName)
			?? $"call_{BuildArchitectureKey(toolName)}";

		return new JsonObject
		{
			["role"] = "tool",
			["tool_call_id"] = toolCallId,
			["name"] = toolName,
			["content"] = ConvertMessageContentToString(message["content"])
		};
	}

	private static string? ResolvePendingToolCallId(
		string toolName,
		IDictionary<string, Queue<string>> pendingToolCallIdsByName)
	{
		if (pendingToolCallIdsByName.TryGetValue(toolName, out var queue) && queue.Count > 0)
			return queue.Dequeue();

		return null;
	}

	private static string ConvertMessageContentToString(JsonNode? content)
	{
		if (content is null)
			return string.Empty;
		if (content is JsonValue value && value.TryGetValue<string>(out var text))
			return text;

		return content.ToJsonString(JsonOptions);
	}

	private static string NormalizeOllamaImageReference(string image)
	{
		var trimmed = image.Trim();
		if (trimmed.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
			|| trimmed.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
			|| trimmed.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
			return trimmed;

		var mediaType = TryDetectImageMediaType(trimmed) ?? "image/png";
		return $"data:{mediaType};base64,{trimmed}";
	}

	private static string? TryDetectImageMediaType(string base64Data)
	{
		try
		{
			var bytes = Convert.FromBase64String(base64Data);
			if (bytes.Length >= 8
				&& bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47)
				return "image/png";
			if (bytes.Length >= 3
				&& bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF)
				return "image/jpeg";
			if (bytes.Length >= 6
				&& bytes[0] == 0x47 && bytes[1] == 0x49 && bytes[2] == 0x46)
				return "image/gif";
			if (bytes.Length >= 12
				&& bytes[0] == 0x52 && bytes[1] == 0x49 && bytes[2] == 0x46 && bytes[3] == 0x46
				&& bytes[8] == 0x57 && bytes[9] == 0x45 && bytes[10] == 0x42 && bytes[11] == 0x50)
				return "image/webp";
		}
		catch
		{
		}

		return null;
	}

	private static JsonNode? TryParseJsonNode(string? json)
	{
		if (string.IsNullOrWhiteSpace(json))
			return null;

		try
		{
			return JsonNode.Parse(json);
		}
		catch
		{
			return null;
		}
	}

	private JsonObject BuildUpstreamEmbeddingsRequestFromOllama(JsonObject source)
	{
		var localModel = source["model"]?.GetValue<string>() ?? string.Empty;
		var input = source["input"]?.DeepClone()
			?? source["prompt"]?.DeepClone()
			?? throw new InvalidOperationException("Ollama embeddings requests must include input or prompt.");

		return new JsonObject
		{
			["model"] = ResolveUpstreamEmbeddingsModel(localModel),
			["input"] = input
		};
	}

	private JsonObject BuildOpenAiChatRequestFromResponses(JsonObject source, bool stream = false)
	{
		var model = source["model"]?.GetValue<string>() ?? string.Empty;
		var messages = BuildMessagesArrayFromResponsesInput(source["input"]);

		var instructions = TryReadString(source["instructions"]);
		if (!string.IsNullOrWhiteSpace(instructions))
		{
			var systemMessage = new JsonObject
			{
				["role"] = "system",
				["content"] = instructions
			};
			messages.Insert(0, systemMessage);
		}

		var request = new JsonObject
		{
			["model"] = ResolveUpstreamChatModel(model),
			["messages"] = messages,
			["stream"] = stream
		};

		if (stream)
			request["stream_options"] = new JsonObject { ["include_usage"] = true };

		if (source["tools"] is JsonArray tools && tools.Count > 0)
			request["tools"] = ConvertResponsesToolsToChatCompletions(tools);
		if (source["tool_choice"] is not null)
			request["tool_choice"] = ConvertResponsesToolChoiceToChatCompletions(source["tool_choice"]);
		if (source["parallel_tool_calls"] is not null)
			request["parallel_tool_calls"] = source["parallel_tool_calls"]!.DeepClone();
		if (source["temperature"] is not null)
			request["temperature"] = source["temperature"]!.DeepClone();
		if (source["top_p"] is not null)
			request["top_p"] = source["top_p"]!.DeepClone();
		if (source["max_output_tokens"] is not null)
			request["max_tokens"] = source["max_output_tokens"]!.DeepClone();

		return request;
	}

	private JsonArray BuildMessagesArrayFromResponsesInput(JsonNode? inputNode)
	{
		if (inputNode is null)
			throw new InvalidOperationException("Responses API requests must include input.");

		if (inputNode is JsonValue inputValue && inputValue.TryGetValue<string>(out var singleInput))
		{
			return
			[
				new JsonObject
				{
					["role"] = "user",
					["content"] = singleInput
				}
			];
		}

		if (inputNode is JsonArray inputArray)
		{
			var result = new JsonArray();
			var pendingToolCalls = new JsonArray();

			foreach (var item in inputArray)
			{
				if (item is not JsonObject messageObject)
					continue;

				var itemType = messageObject["type"]?.GetValue<string>() ?? string.Empty;

				if (itemType.Equals("function_call", StringComparison.OrdinalIgnoreCase))
				{
					var callId = TryReadString(messageObject["call_id"])
						?? TryReadString(messageObject["id"])
						?? $"call_{Guid.NewGuid():N}";
					pendingToolCalls.Add(new JsonObject
					{
						["id"] = callId,
						["type"] = "function",
						["function"] = new JsonObject
						{
							["name"] = TryReadString(messageObject["name"]) ?? "tool",
							["arguments"] = TryReadString(messageObject["arguments"]) ?? "{}"
						}
					});
					continue;
				}

				if (itemType.Equals("function_call_output", StringComparison.OrdinalIgnoreCase))
				{
					if (pendingToolCalls.Count > 0)
					{
						result.Add(new JsonObject
						{
							["role"] = "assistant",
							["content"] = (JsonNode?)null,
							["tool_calls"] = pendingToolCalls
						});
						pendingToolCalls = new JsonArray();
					}

					var toolCallId = TryReadString(messageObject["call_id"])
						?? $"call_{Guid.NewGuid():N}";
					var output = TryReadString(messageObject["output"]) ?? string.Empty;
					result.Add(new JsonObject
					{
						["role"] = "tool",
						["tool_call_id"] = toolCallId,
						["content"] = output
					});
					continue;
				}

				if (pendingToolCalls.Count > 0)
				{
					result.Add(new JsonObject
					{
						["role"] = "assistant",
						["content"] = (JsonNode?)null,
						["tool_calls"] = pendingToolCalls
					});
					pendingToolCalls = new JsonArray();
				}

				if (messageObject["role"] is not null && messageObject["content"] is not null)
				{
					result.Add(new JsonObject
					{
						["role"] = messageObject["role"]!.DeepClone(),
						["content"] = ConvertResponsesContentToMessageContent(messageObject["content"])
					});
					continue;
				}

				if (itemType.Equals("message", StringComparison.OrdinalIgnoreCase))
				{
					result.Add(new JsonObject
					{
						["role"] = messageObject["role"]?.GetValue<string>() ?? "user",
						["content"] = ConvertResponsesContentToMessageContent(messageObject["content"])
					});
				}
			}

			if (pendingToolCalls.Count > 0)
			{
				result.Add(new JsonObject
				{
					["role"] = "assistant",
					["content"] = (JsonNode?)null,
					["tool_calls"] = pendingToolCalls
				});
			}

			if (result.Count > 0)
				return result;
		}

		throw new InvalidOperationException("Unsupported Responses API input format.");
	}

	private JsonNode ConvertResponsesContentToMessageContent(JsonNode? contentNode)
	{
		if (contentNode is null)
			return string.Empty;

		if (contentNode is JsonValue value && value.TryGetValue<string>(out var text))
			return text;

		if (contentNode is JsonArray parts)
		{
			var textParts = parts
				.OfType<JsonObject>()
				.Select(static part => part["text"]?.GetValue<string>())
				.Where(static partText => !string.IsNullOrWhiteSpace(partText));
			return string.Join(Environment.NewLine, textParts!);
		}

		return contentNode.DeepClone();
	}

	private JsonArray ConvertResponsesToolsToChatCompletions(JsonArray tools)
	{
		var result = new JsonArray();
		foreach (var toolNode in tools.OfType<JsonObject>())
		{
			if (toolNode["function"] is JsonObject)
			{
				result.Add(toolNode.DeepClone());
				continue;
			}

			result.Add(new JsonObject
			{
				["type"] = "function",
				["function"] = new JsonObject
				{
					["name"] = TryReadString(toolNode["name"]) ?? "tool",
					["description"] = TryReadString(toolNode["description"]) ?? string.Empty,
					["parameters"] = toolNode["parameters"]?.DeepClone() ?? new JsonObject(),
					["strict"] = toolNode["strict"]?.DeepClone()
				}
			});
		}

		return result;
	}

	private static JsonNode ConvertResponsesToolChoiceToChatCompletions(JsonNode? toolChoice)
	{
		if (toolChoice is JsonValue v && v.TryGetValue<string>(out var s))
			return s;

		if (toolChoice is JsonObject obj && obj["name"] is not null)
		{
			return new JsonObject
			{
				["type"] = "function",
				["function"] = new JsonObject
				{
					["name"] = obj["name"]!.DeepClone()
				}
			};
		}

		return toolChoice?.DeepClone() ?? "auto";
	}

	private async Task WriteResponsesStreamAsync(
		HttpListenerResponse response,
		HttpResponseMessage upstreamResponse,
		string requestedModel,
		CancellationToken ct)
	{
		if (!upstreamResponse.IsSuccessStatusCode)
		{
			var errorBody = await upstreamResponse.Content.ReadAsStringAsync(ct);
			await WriteErrorAsync(response, "/v1/responses", upstreamResponse.StatusCode,
				await ExtractUpstreamErrorAsync(upstreamResponse.StatusCode, errorBody),
				"openai", upstreamResponse.StatusCode, errorBody, null);
			return;
		}

		response.StatusCode = (int)HttpStatusCode.OK;
		response.ContentType = "text/event-stream";
		response.SendChunked = true;
		response.Headers["Cache-Control"] = "no-cache";
		response.Headers["X-Accel-Buffering"] = "no";

		var output = response.OutputStream;
		using var writer = new StreamWriter(output, new UTF8Encoding(false)) { AutoFlush = true, NewLine = "\n" };
		await using var upstreamStream = await upstreamResponse.Content.ReadAsStreamAsync(ct);
		using var reader = new StreamReader(upstreamStream);

		var responseId = $"resp_{Guid.NewGuid():N}";
		var messageItemId = $"msg_{Guid.NewGuid():N}";
		var createdAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
		var textAccumulator = new StringBuilder();
		var toolCallAccumulators = new Dictionary<int, StreamingToolCallAccumulator>();
		var emittedToolCallItems = new HashSet<int>();
		var messageItemEmitted = false;
		var messageItemFinalized = false;
		var contentPartEmitted = false;
		var messageOutputIndex = 0;
		JsonObject? usageStats = null;

		await WriteSseEventAsync(writer, "response.created", new JsonObject
		{
			["type"] = "response.created",
			["response"] = new JsonObject
			{
				["id"] = responseId,
				["object"] = "response",
				["created_at"] = createdAt,
				["status"] = "in_progress",
				["model"] = requestedModel,
				["output"] = new JsonArray()
			}
		});

		while (!ct.IsCancellationRequested)
		{
			var line = await reader.ReadLineAsync(ct);
			if (line is null)
				break;

			if (string.IsNullOrWhiteSpace(line) || !line.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
				continue;

			var payload = line[5..].Trim();
			if (payload.Length == 0)
				continue;

			if (payload.Equals("[DONE]", StringComparison.Ordinal))
				break;

			var eventJson = JsonNode.Parse(payload)?.AsObject();
			if (eventJson is null)
				continue;

			var delta = eventJson["choices"]?[0]?["delta"]?.AsObject();
			var finishReason = TryReadString(eventJson["choices"]?[0]?["finish_reason"]);

			if (delta is not null)
			{
				var content = TryReadString(delta["content"]);

				if (!string.IsNullOrEmpty(content))
				{
					if (!messageItemEmitted)
					{
						await WriteSseEventAsync(writer, "response.output_item.added", new JsonObject
						{
							["type"] = "response.output_item.added",
							["output_index"] = messageOutputIndex,
							["item"] = new JsonObject
							{
								["type"] = "message",
								["id"] = messageItemId,
								["status"] = "in_progress",
								["role"] = "assistant",
								["content"] = new JsonArray()
							}
						});
						messageItemEmitted = true;
					}

					if (!contentPartEmitted)
					{
						await WriteSseEventAsync(writer, "response.content_part.added", new JsonObject
						{
							["type"] = "response.content_part.added",
							["item_id"] = messageItemId,
							["output_index"] = messageOutputIndex,
							["content_index"] = 0,
							["part"] = new JsonObject
							{
								["type"] = "output_text",
								["text"] = "",
								["annotations"] = new JsonArray()
							}
						});
						contentPartEmitted = true;
					}

					await WriteSseEventAsync(writer, "response.output_text.delta", new JsonObject
					{
						["type"] = "response.output_text.delta",
						["item_id"] = messageItemId,
						["output_index"] = messageOutputIndex,
						["content_index"] = 0,
						["delta"] = content
					});
					textAccumulator.Append(content);
				}

				if (delta["tool_calls"] is JsonArray deltaToolCalls)
				{
					if (messageItemEmitted && !messageItemFinalized)
					{
						await FinalizeResponsesMessageItemAsync(writer, messageItemId, messageOutputIndex, textAccumulator.ToString(), contentPartEmitted);
						messageItemFinalized = true;
						messageOutputIndex++;
					}

					foreach (var tc in deltaToolCalls.OfType<JsonObject>())
					{
						var tcIndex = (int)(TryReadInt64(tc["index"]) ?? 0);
						if (!toolCallAccumulators.TryGetValue(tcIndex, out var acc))
						{
							acc = new StreamingToolCallAccumulator();
							toolCallAccumulators[tcIndex] = acc;
						}

						acc.Id = TryReadString(tc["id"]) ?? acc.Id;
						var function = tc["function"] as JsonObject;
						if (function is null)
							continue;

						acc.Name = TryReadString(function["name"]) ?? acc.Name;
						var args = TryReadString(function["arguments"]);

						if (!emittedToolCallItems.Contains(tcIndex) && !string.IsNullOrWhiteSpace(acc.Name))
						{
							if (string.IsNullOrWhiteSpace(acc.Id))
								acc.Id = $"fc_{Guid.NewGuid():N}";

							await WriteSseEventAsync(writer, "response.output_item.added", new JsonObject
							{
								["type"] = "response.output_item.added",
								["output_index"] = messageOutputIndex + tcIndex,
								["item"] = new JsonObject
								{
									["type"] = "function_call",
									["id"] = acc.Id,
									["call_id"] = acc.Id,
									["name"] = acc.Name,
									["arguments"] = "",
									["status"] = "in_progress"
								}
							});
							emittedToolCallItems.Add(tcIndex);
						}

						if (!string.IsNullOrEmpty(args))
						{
							acc.Arguments.Append(args);
							if (emittedToolCallItems.Contains(tcIndex))
							{
								await WriteSseEventAsync(writer, "response.function_call_arguments.delta", new JsonObject
								{
									["type"] = "response.function_call_arguments.delta",
									["item_id"] = acc.Id,
									["output_index"] = messageOutputIndex + tcIndex,
									["delta"] = args
								});
							}
						}
					}
				}
			}

			var usage = eventJson["usage"]?.AsObject();
			if (usage is not null)
				usageStats = usage.DeepClone().AsObject();

			if (!string.IsNullOrWhiteSpace(finishReason))
				break;
		}

		if (messageItemEmitted && !messageItemFinalized)
		{
			await FinalizeResponsesMessageItemAsync(writer, messageItemId, messageOutputIndex, textAccumulator.ToString(), contentPartEmitted);
			messageOutputIndex++;
		}

		var fullOutput = new JsonArray();
		if (messageItemEmitted)
		{
			fullOutput.Add(new JsonObject
			{
				["type"] = "message",
				["id"] = messageItemId,
				["status"] = "completed",
				["role"] = "assistant",
				["content"] = new JsonArray
				{
					new JsonObject
					{
						["type"] = "output_text",
						["text"] = textAccumulator.ToString(),
						["annotations"] = new JsonArray()
					}
				}
			});
		}

		foreach (var pair in toolCallAccumulators.OrderBy(static p => p.Key))
		{
			var acc = pair.Value;
			if (string.IsNullOrWhiteSpace(acc.Id))
				acc.Id = $"fc_{Guid.NewGuid():N}";
			var fullArgs = acc.Arguments.ToString();
			var itemOutputIndex = messageOutputIndex + pair.Key;

			await WriteSseEventAsync(writer, "response.function_call_arguments.done", new JsonObject
			{
				["type"] = "response.function_call_arguments.done",
				["item_id"] = acc.Id,
				["output_index"] = itemOutputIndex,
				["arguments"] = fullArgs
			});

			var completedItem = new JsonObject
			{
				["type"] = "function_call",
				["id"] = acc.Id,
				["call_id"] = acc.Id,
				["name"] = acc.Name,
				["arguments"] = fullArgs,
				["status"] = "completed"
			};
			await WriteSseEventAsync(writer, "response.output_item.done", new JsonObject
			{
				["type"] = "response.output_item.done",
				["output_index"] = itemOutputIndex,
				["item"] = completedItem
			});

			fullOutput.Add(completedItem.DeepClone());
		}

		var completedResponse = new JsonObject
		{
			["id"] = responseId,
			["object"] = "response",
			["created_at"] = createdAt,
			["status"] = "completed",
			["model"] = requestedModel,
			["output"] = fullOutput
		};

		if (usageStats is not null)
		{
			completedResponse["usage"] = new JsonObject
			{
				["input_tokens"] = usageStats["prompt_tokens"]?.DeepClone() ?? 0,
				["output_tokens"] = usageStats["completion_tokens"]?.DeepClone() ?? 0,
				["total_tokens"] = usageStats["total_tokens"]?.DeepClone() ?? 0
			};
		}

		await WriteSseEventAsync(writer, "response.completed", new JsonObject
		{
			["type"] = "response.completed",
			["response"] = completedResponse
		});

		await output.FlushAsync(ct);
		TryCloseResponse(response);
	}

	private async Task FinalizeResponsesMessageItemAsync(
		StreamWriter writer,
		string messageItemId,
		int outputIndex,
		string text,
		bool contentPartEmitted)
	{
		if (contentPartEmitted)
		{
			await WriteSseEventAsync(writer, "response.output_text.done", new JsonObject
			{
				["type"] = "response.output_text.done",
				["item_id"] = messageItemId,
				["output_index"] = outputIndex,
				["content_index"] = 0,
				["text"] = text
			});

			await WriteSseEventAsync(writer, "response.content_part.done", new JsonObject
			{
				["type"] = "response.content_part.done",
				["item_id"] = messageItemId,
				["output_index"] = outputIndex,
				["content_index"] = 0,
				["part"] = new JsonObject
				{
					["type"] = "output_text",
					["text"] = text,
					["annotations"] = new JsonArray()
				}
			});
		}

		await WriteSseEventAsync(writer, "response.output_item.done", new JsonObject
		{
			["type"] = "response.output_item.done",
			["output_index"] = outputIndex,
			["item"] = new JsonObject
			{
				["type"] = "message",
				["id"] = messageItemId,
				["status"] = "completed",
				["role"] = "assistant",
				["content"] = new JsonArray
				{
					new JsonObject
					{
						["type"] = "output_text",
						["text"] = text,
						["annotations"] = new JsonArray()
					}
				}
			}
		});
	}

	private static async Task WriteSseEventAsync(StreamWriter writer, string eventName, JsonObject data)
	{
		await writer.WriteAsync("event: ");
		await writer.WriteLineAsync(eventName);
		await writer.WriteAsync("data: ");
		await writer.WriteLineAsync(data.ToJsonString(JsonOptions));
		await writer.WriteLineAsync();
	}

	private JsonObject BuildAnthropicMessagesRequestFromGenerate(JsonObject source)
	{
		return BuildAnthropicMessagesRequest(
			ResolveUpstreamChatModel(source["model"]?.GetValue<string>()),
			BuildMessagesForGenerate(source),
			source,
			allowTools: false);
	}

	private JsonObject BuildAnthropicMessagesRequestFromOllamaChat(JsonObject source)
	{
		if (source["messages"] is not JsonArray messages || messages.Count == 0)
			throw new InvalidOperationException("Ollama chat requests must include messages.");

		return BuildAnthropicMessagesRequest(
			ResolveUpstreamChatModel(source["model"]?.GetValue<string>()),
			messages,
			source,
			allowTools: false);
	}

	private JsonObject BuildAnthropicMessagesRequestFromOpenAi(JsonObject source)
	{
		if (source["messages"] is not JsonArray messages || messages.Count == 0)
			throw new InvalidOperationException("Chat completions requests must include messages.");

		return BuildAnthropicMessagesRequest(
			ResolveUpstreamChatModel(source["model"]?.GetValue<string>()),
			messages,
			source,
			allowTools: true);
	}

	private JsonObject BuildAnthropicMessagesRequestFromResponses(JsonObject source)
	{
		return BuildAnthropicMessagesRequest(
			ResolveUpstreamChatModel(source["model"]?.GetValue<string>()),
			BuildMessagesArrayFromResponsesInput(source["input"]),
			source,
			allowTools: true);
	}

	private JsonArray BuildMessagesForGenerate(JsonObject source)
	{
		var messages = new JsonArray();
		var system = source["system"]?.GetValue<string>();
		if (!string.IsNullOrWhiteSpace(system))
		{
			messages.Add(new JsonObject
			{
				["role"] = "system",
				["content"] = system
			});
		}

		var prompt = source["prompt"]?.GetValue<string>();
		if (!string.IsNullOrWhiteSpace(prompt))
		{
			messages.Add(new JsonObject
			{
				["role"] = "user",
				["content"] = prompt
			});
		}

		return messages;
	}

	private JsonObject BuildAnthropicMessagesRequest(string upstreamModel, JsonArray sourceMessages, JsonObject source, bool allowTools)
	{
		var anthropicMessages = new JsonArray();
		var systemParts = new List<string>();

		foreach (var message in sourceMessages.OfType<JsonObject>())
		{
			var role = message["role"]?.GetValue<string>() ?? "user";
			var content = ExtractTextContent(message["content"]);

			if (role.Equals("system", StringComparison.OrdinalIgnoreCase))
			{
				if (!string.IsNullOrWhiteSpace(content))
					systemParts.Add(content);
				continue;
			}

			anthropicMessages.Add(new JsonObject
			{
				["role"] = role.Equals("assistant", StringComparison.OrdinalIgnoreCase) ? "assistant" : "user",
				["content"] = content
			});
		}

		var request = new JsonObject
		{
			["model"] = upstreamModel,
			["messages"] = anthropicMessages,
			["max_tokens"] = source["max_tokens"]?.GetValue<int?>()
				?? source["max_output_tokens"]?.GetValue<int?>()
				?? source["options"]?["num_predict"]?.GetValue<int?>()
				?? 1024
		};

		if (systemParts.Count > 0)
			request["system"] = string.Join(Environment.NewLine, systemParts);
		if (source["temperature"] is not null)
			request["temperature"] = source["temperature"]!.DeepClone();

		if (allowTools && source["tools"] is JsonArray tools && tools.Count > 0)
			request["tools"] = ConvertOpenAiToolsToAnthropic(tools);

		return request;
	}

	private JsonArray ConvertOpenAiToolsToAnthropic(JsonArray tools)
	{
		var anthropicTools = new JsonArray();
		foreach (var toolNode in tools.OfType<JsonObject>())
		{
			var function = toolNode["function"] as JsonObject;
			if (function is null)
				continue;

			anthropicTools.Add(new JsonObject
			{
				["name"] = function["name"]?.GetValue<string>() ?? "tool",
				["description"] = function["description"]?.GetValue<string>() ?? string.Empty,
				["input_schema"] = function["parameters"]?.DeepClone() ?? new JsonObject()
			});
		}

		return anthropicTools;
	}

	private static void CopyCommonGenerationOptions(JsonObject source, JsonObject target)
	{
		foreach (var property in new[] { "temperature", "top_p", "max_tokens", "stop", "tools" })
		{
			if (source[property] is not null)
				target[property] = source[property]!.DeepClone();
		}

		if (source["options"] is JsonObject options)
		{
			if (options["temperature"] is not null)
				target["temperature"] = options["temperature"]!.DeepClone();
			if (options["top_p"] is not null)
				target["top_p"] = options["top_p"]!.DeepClone();
			if (options["stop"] is not null)
				target["stop"] = options["stop"]!.DeepClone();
			if (options["num_predict"] is not null)
				target["max_tokens"] = options["num_predict"]!.DeepClone();
		}
	}

	private JsonObject ConvertOpenAiChatResponseToOllama(JsonObject parsed, string localModel, bool isChat)
	{
		var content = ExtractAssistantContent(parsed);
		var finishReason = parsed["choices"]?[0]?["finish_reason"]?.GetValue<string>();
		var usage = parsed["usage"]?.AsObject();
		var modelName = string.IsNullOrWhiteSpace(localModel) ? (parsed["model"]?.GetValue<string>() ?? ResolveUpstreamChatModel(string.Empty)) : localModel;

		if (isChat)
		{
			var assistantMessage = new JsonObject
			{
				["role"] = "assistant",
				["content"] = content
			};
			if (TryConvertOpenAiToolCalls(parsed, out var toolCalls))
				assistantMessage["tool_calls"] = toolCalls;

			return new JsonObject
			{
				["model"] = modelName,
				["created_at"] = DateTimeOffset.UtcNow.ToString("O"),
				["message"] = assistantMessage,
				["done"] = true,
				["done_reason"] = finishReason ?? "stop",
				["prompt_eval_count"] = usage?["prompt_tokens"]?.GetValue<int>() ?? 0,
				["eval_count"] = usage?["completion_tokens"]?.GetValue<int>() ?? 0
			};
		}

		return new JsonObject
		{
			["model"] = modelName,
			["created_at"] = DateTimeOffset.UtcNow.ToString("O"),
			["response"] = content,
			["done"] = true,
			["done_reason"] = finishReason ?? "stop",
			["prompt_eval_count"] = usage?["prompt_tokens"]?.GetValue<int>() ?? 0,
			["eval_count"] = usage?["completion_tokens"]?.GetValue<int>() ?? 0,
			["total_duration"] = 0,
			["load_duration"] = 0,
			["prompt_eval_duration"] = 0,
			["eval_duration"] = 0
		};
	}

	private JsonObject ConvertAnthropicResponseToOllama(JsonObject parsed, string localModel, bool isChat)
	{
		var content = ExtractAnthropicText(parsed);
		var finishReason = parsed["stop_reason"]?.GetValue<string>() ?? "stop";
		var usage = parsed["usage"]?.AsObject();
		var modelName = string.IsNullOrWhiteSpace(localModel)
			? parsed["model"]?.GetValue<string>() ?? ResolveUpstreamChatModel(string.Empty)
			: localModel;

		if (isChat)
		{
			var assistantMessage = new JsonObject
			{
				["role"] = "assistant",
				["content"] = content
			};
			if (TryConvertAnthropicToolCalls(parsed, out var anthropicToolCalls))
				assistantMessage["tool_calls"] = ConvertOpenAiToolCallsToOllama(anthropicToolCalls);

			return new JsonObject
			{
				["model"] = modelName,
				["created_at"] = DateTimeOffset.UtcNow.ToString("O"),
				["message"] = assistantMessage,
				["done"] = true,
				["done_reason"] = finishReason,
				["prompt_eval_count"] = usage?["input_tokens"]?.GetValue<int>() ?? 0,
				["eval_count"] = usage?["output_tokens"]?.GetValue<int>() ?? 0
			};
		}

		return new JsonObject
		{
			["model"] = modelName,
			["created_at"] = DateTimeOffset.UtcNow.ToString("O"),
			["response"] = content,
			["done"] = true,
			["done_reason"] = finishReason,
			["prompt_eval_count"] = usage?["input_tokens"]?.GetValue<int>() ?? 0,
			["eval_count"] = usage?["output_tokens"]?.GetValue<int>() ?? 0,
			["total_duration"] = 0,
			["load_duration"] = 0,
			["prompt_eval_duration"] = 0,
			["eval_duration"] = 0
		};
	}

	private JsonObject ConvertAnthropicResponseToOpenAiChat(JsonObject parsed, string requestedModel)
	{
		var model = string.IsNullOrWhiteSpace(requestedModel)
			? parsed["model"]?.GetValue<string>() ?? ResolveUpstreamChatModel(string.Empty)
			: requestedModel;
		var usage = parsed["usage"]?.AsObject();
		var assistantMessage = new JsonObject
		{
			["role"] = "assistant",
			["content"] = ExtractAnthropicText(parsed)
		};

		if (TryConvertAnthropicToolCalls(parsed, out var toolCalls))
			assistantMessage["tool_calls"] = toolCalls;

		return new JsonObject
		{
			["id"] = parsed["id"]?.GetValue<string>() ?? $"chatcmpl-{Guid.NewGuid():N}",
			["object"] = "chat.completion",
			["created"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
			["model"] = model,
			["choices"] = new JsonArray
			{
				new JsonObject
				{
					["index"] = 0,
					["message"] = assistantMessage,
					["finish_reason"] = parsed["stop_reason"]?.GetValue<string>() ?? "stop"
				}
			},
			["usage"] = new JsonObject
			{
				["prompt_tokens"] = usage?["input_tokens"]?.GetValue<int>() ?? 0,
				["completion_tokens"] = usage?["output_tokens"]?.GetValue<int>() ?? 0,
				["total_tokens"] = (usage?["input_tokens"]?.GetValue<int>() ?? 0) + (usage?["output_tokens"]?.GetValue<int>() ?? 0)
			}
		};
	}

	private JsonObject ConvertOpenAiChatResponseToResponses(JsonObject parsed, string requestedModel)
	{
		var message = parsed["choices"]?[0]?["message"]?.AsObject() ?? new JsonObject();
		var text = message["content"]?.GetValue<string>() ?? string.Empty;
		var output = new JsonArray
		{
			new JsonObject
			{
				["id"] = $"msg_{Guid.NewGuid():N}",
				["type"] = "message",
				["status"] = "completed",
				["role"] = "assistant",
				["content"] = new JsonArray
				{
					new JsonObject
					{
						["type"] = "output_text",
						["text"] = text,
						["annotations"] = new JsonArray()
					}
				}
			}
		};

		if (message["tool_calls"] is JsonArray toolCalls && toolCalls.Count > 0)
		{
			foreach (var item in ConvertOpenAiToolCallsToResponses(toolCalls))
				output.Add(item?.DeepClone());
		}

		return new JsonObject
		{
			["id"] = parsed["id"]?.GetValue<string>() ?? $"resp_{Guid.NewGuid():N}",
			["object"] = "response",
			["created_at"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
			["status"] = "completed",
			["model"] = string.IsNullOrWhiteSpace(requestedModel) ? parsed["model"]?.GetValue<string>() ?? ResolveUpstreamChatModel(string.Empty) : requestedModel,
			["output"] = output,
			["output_text"] = text
		};
	}

	private JsonObject ConvertAnthropicResponseToResponses(JsonObject parsed, string requestedModel)
	{
		var text = ExtractAnthropicText(parsed);
		var output = new JsonArray
		{
			new JsonObject
			{
				["id"] = $"msg_{Guid.NewGuid():N}",
				["type"] = "message",
				["status"] = "completed",
				["role"] = "assistant",
				["content"] = new JsonArray
				{
					new JsonObject
					{
						["type"] = "output_text",
						["text"] = text,
						["annotations"] = new JsonArray()
					}
				}
			}
		};

		if (TryConvertAnthropicToolCalls(parsed, out var toolCalls))
		{
			foreach (var item in ConvertOpenAiToolCallsToResponses(toolCalls))
				output.Add(item?.DeepClone());
		}

		return new JsonObject
		{
			["id"] = parsed["id"]?.GetValue<string>() ?? $"resp_{Guid.NewGuid():N}",
			["object"] = "response",
			["created_at"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
			["status"] = "completed",
			["model"] = string.IsNullOrWhiteSpace(requestedModel) ? parsed["model"]?.GetValue<string>() ?? ResolveUpstreamChatModel(string.Empty) : requestedModel,
			["output"] = output,
			["output_text"] = text
		};
	}

	private JsonNode ConvertOpenAiEmbeddingsResponseToOllama(JsonObject parsed, string localModel, string path)
	{
		var data = parsed["data"] as JsonArray ?? [];
		var modelName = string.IsNullOrWhiteSpace(localModel)
			? parsed["model"]?.GetValue<string>() ?? ResolveUpstreamEmbeddingsModel(string.Empty)
			: localModel;

		if (path.Equals("/api/embed", StringComparison.OrdinalIgnoreCase))
		{
			return new JsonObject
			{
				["model"] = modelName,
				["embeddings"] = new JsonArray(data.OfType<JsonObject>()
					.Select(static item => item["embedding"]?.DeepClone() ?? new JsonArray())
					.ToArray()),
				["total_duration"] = 0,
				["load_duration"] = 0,
				["prompt_eval_count"] = 0
			};
		}

		if (data.Count == 1)
		{
			return new JsonObject
			{
				["embedding"] = data[0]?["embedding"]?.DeepClone() ?? new JsonArray()
			};
		}

		return new JsonObject
		{
			["model"] = modelName,
			["embeddings"] = new JsonArray(data.OfType<JsonObject>()
				.Select(static item => item["embedding"]?.DeepClone() ?? new JsonArray())
				.ToArray())
		};
	}

	private static string ExtractAssistantContent(JsonObject parsed)
	{
		var messageContent = ExtractTextContent(parsed["choices"]?[0]?["message"]?["content"]);
		if (!string.IsNullOrWhiteSpace(messageContent))
			return messageContent;

		return ExtractTextContent(parsed["choices"]?[0]?["delta"]?["content"]);
	}

	private static string ExtractAnthropicText(JsonObject parsed)
	{
		if (parsed["content"] is not JsonArray contentArray)
			return string.Empty;

		return string.Join(Environment.NewLine, contentArray
			.OfType<JsonObject>()
			.Where(static item => (item["type"]?.GetValue<string>() ?? string.Empty).Equals("text", StringComparison.OrdinalIgnoreCase))
			.Select(static item => item["text"]?.GetValue<string>() ?? string.Empty));
	}

	private bool TryConvertAnthropicToolCalls(JsonObject parsed, out JsonArray toolCalls)
	{
		toolCalls = [];
		if (parsed["content"] is not JsonArray contentArray)
			return false;

		foreach (var item in contentArray.OfType<JsonObject>())
		{
			if (!(item["type"]?.GetValue<string>() ?? string.Empty).Equals("tool_use", StringComparison.OrdinalIgnoreCase))
				continue;

			toolCalls.Add(new JsonObject
			{
				["id"] = item["id"]?.GetValue<string>() ?? $"call_{Guid.NewGuid():N}",
				["type"] = "function",
				["function"] = new JsonObject
				{
					["name"] = item["name"]?.GetValue<string>() ?? "tool",
					["arguments"] = item["input"]?.ToJsonString(JsonOptions) ?? "{}"
				}
			});
		}

		return toolCalls.Count > 0;
	}

	private JsonArray ConvertOpenAiToolCallsToResponses(JsonArray toolCalls)
	{
		var output = new JsonArray();
		foreach (var item in toolCalls.OfType<JsonObject>())
		{
			output.Add(new JsonObject
			{
				["type"] = "function_call",
				["id"] = item["id"]?.GetValue<string>() ?? $"fc_{Guid.NewGuid():N}",
				["call_id"] = item["id"]?.GetValue<string>() ?? $"fc_{Guid.NewGuid():N}",
				["name"] = item["function"]?["name"]?.GetValue<string>() ?? "tool",
				["arguments"] = item["function"]?["arguments"]?.GetValue<string>() ?? "{}"
			});
		}

		return output;
	}

	private static string ExtractTextContent(JsonNode? node)
	{
		if (node is null)
			return string.Empty;
		if (node is JsonValue value && value.TryGetValue<string>(out var text))
			return text;
		if (node is JsonArray array)
		{
			return string.Join(Environment.NewLine, array
				.OfType<JsonObject>()
				.Select(static item => item["text"]?.GetValue<string>() ?? item["content"]?.GetValue<string>() ?? string.Empty)
				.Where(static item => !string.IsNullOrWhiteSpace(item)));
		}

		return node.ToJsonString(JsonOptions);
	}

	private JsonObject ParseJsonObject(string body, string description)
	{
		return JsonNode.Parse(body)?.AsObject()
			?? throw new InvalidOperationException($"{description} was not a JSON object.");
	}

	private void LogRequestIfEnabled(string operation, JsonNode payload)
	{
		var message = $"request {operation}";
		if (_settings.RequestResponseLogging?.IncludeBodies == true)
			message += $": {TruncateForLog(payload.ToJsonString(JsonOptions))}";
		LogInfo(message);
	}

	private void LogResponseIfEnabled(string operation, HttpStatusCode statusCode, string body)
	{
		var message = $"response {operation}: {(int)statusCode} {statusCode}";
		if (_settings.RequestResponseLogging?.IncludeBodies == true)
			message += $" | {TruncateForLog(body)}";
		LogInfo(message);
	}

	private void LogIncomingRequest(long requestId, HttpListenerRequest request, string path)
	{
		var query = request.Url?.Query ?? string.Empty;
		var contentType = string.IsNullOrWhiteSpace(request.ContentType) ? "-" : request.ContentType;
		var contentLength = request.HasEntityBody ? request.ContentLength64 : 0;
		LogInfo($"incoming #{requestId}: {request.HttpMethod} {path}{query} | content-type={contentType} | length={contentLength}");
	}

	private string TruncateForLog(string text)
	{
		var max = Math.Max(256, _settings.RequestResponseLogging?.MaxBodyCharacters ?? 4000);
		if (text.Length <= max)
			return text;

		return $"{text[..max]} ...(truncated)";
	}

	private async Task WriteOllamaStreamingResponseAsync(
		HttpListenerResponse response,
		HttpResponseMessage upstreamResponse,
		string localModel,
		bool isChat,
		CancellationToken ct)
	{
		if (!upstreamResponse.IsSuccessStatusCode)
		{
			var errorBody = await upstreamResponse.Content.ReadAsStringAsync(ct);
			await WriteErrorAsync(response, isChat ? "/api/chat" : "/api/generate", upstreamResponse.StatusCode,
				await ExtractUpstreamErrorAsync(upstreamResponse.StatusCode, errorBody),
				"ollama", upstreamResponse.StatusCode, errorBody, localModel);
			return;
		}

		response.StatusCode = (int)HttpStatusCode.OK;
		response.ContentType = "application/x-ndjson";
		response.SendChunked = true;

		var output = response.OutputStream;
		await using var upstreamStream = await upstreamResponse.Content.ReadAsStreamAsync(ct);
		using var reader = new StreamReader(upstreamStream);
		using var writer = new StreamWriter(output, new UTF8Encoding(false)) { AutoFlush = true };

		var createdAt = DateTimeOffset.UtcNow.ToString("O");
		var streamedToolCalls = new Dictionary<int, StreamingToolCallAccumulator>();
		while (!ct.IsCancellationRequested)
		{
			var line = await reader.ReadLineAsync(ct);
			if (line is null)
				break;

			if (string.IsNullOrWhiteSpace(line) || !line.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
				continue;

			var payload = line[5..].Trim();
			if (payload.Length == 0)
				continue;

			if (payload.Equals("[DONE]", StringComparison.Ordinal))
			{
				if (isChat && streamedToolCalls.Count > 0)
				{
					await writer.WriteLineAsync(BuildOllamaStreamChunk(
						localModel,
						createdAt,
						isChat,
						string.Empty,
						done: false,
						doneReason: null,
						toolCalls: BuildStreamedOllamaToolCalls(streamedToolCalls)).ToJsonString(JsonOptions));
				}

				await writer.WriteLineAsync(BuildOllamaStreamChunk(localModel, createdAt, isChat, string.Empty, true, "stop").ToJsonString(JsonOptions));
				break;
			}

			var eventJson = JsonNode.Parse(payload)?.AsObject();
			if (eventJson is null)
				continue;

			if (isChat)
				AccumulateStreamingToolCalls(eventJson["choices"]?[0]?["delta"]?["tool_calls"] as JsonArray, streamedToolCalls);
			if (isChat && eventJson["choices"]?[0]?["message"]?["tool_calls"] is JsonArray fullToolCalls)
				ReplaceStreamingToolCalls(fullToolCalls, streamedToolCalls);

			var chunkText = eventJson["choices"]?[0]?["delta"]?["content"]?.GetValue<string>()
				?? eventJson["choices"]?[0]?["message"]?["content"]?.GetValue<string>()
				?? string.Empty;
			var finishReason = eventJson["choices"]?[0]?["finish_reason"]?.GetValue<string>();

			if (chunkText.Length > 0)
				await writer.WriteLineAsync(BuildOllamaStreamChunk(localModel, createdAt, isChat, chunkText, false, finishReason).ToJsonString(JsonOptions));

			if (!string.IsNullOrWhiteSpace(finishReason))
			{
				if (isChat && streamedToolCalls.Count > 0)
				{
					await writer.WriteLineAsync(BuildOllamaStreamChunk(
						localModel,
						createdAt,
						isChat,
						string.Empty,
						done: false,
						doneReason: null,
						toolCalls: BuildStreamedOllamaToolCalls(streamedToolCalls)).ToJsonString(JsonOptions));
				}

				await writer.WriteLineAsync(BuildOllamaStreamChunk(localModel, createdAt, isChat, string.Empty, true, finishReason).ToJsonString(JsonOptions));
				break;
			}
		}

		await output.FlushAsync(ct);
		TryCloseResponse(response);
	}

	private static JsonObject BuildOllamaStreamChunk(
		string model,
		string createdAt,
		bool isChat,
		string content,
		bool done,
		string? doneReason,
		JsonArray? toolCalls = null)
	{
		if (isChat)
		{
			var message = new JsonObject
			{
				["role"] = "assistant",
				["content"] = content
			};
			if (toolCalls is not null && toolCalls.Count > 0)
				message["tool_calls"] = toolCalls;

			return new JsonObject
			{
				["model"] = model,
				["created_at"] = createdAt,
				["message"] = message,
				["done"] = done,
				["done_reason"] = doneReason
			};
		}

		return new JsonObject
		{
			["model"] = model,
			["created_at"] = createdAt,
			["response"] = content,
			["done"] = done,
			["done_reason"] = doneReason
		};
	}

	private static void AccumulateStreamingToolCalls(
		JsonArray? deltaToolCalls,
		IDictionary<int, StreamingToolCallAccumulator> streamedToolCalls)
	{
		if (deltaToolCalls is null)
			return;

		foreach (var item in deltaToolCalls.OfType<JsonObject>())
		{
			var index = (int)(TryReadInt64(item["index"]) ?? 0);
			if (!streamedToolCalls.TryGetValue(index, out var accumulator))
			{
				accumulator = new StreamingToolCallAccumulator();
				streamedToolCalls[index] = accumulator;
			}

			accumulator.Id = TryReadString(item["id"]) ?? accumulator.Id;
			var function = item["function"] as JsonObject;
			if (function is null)
				continue;

			accumulator.Name = function["name"]?.GetValue<string>() ?? accumulator.Name;
			var arguments = TryReadString(function["arguments"]);
			if (!string.IsNullOrWhiteSpace(arguments))
				accumulator.Arguments.Append(arguments);
		}
	}

	private static void ReplaceStreamingToolCalls(
		JsonArray toolCalls,
		IDictionary<int, StreamingToolCallAccumulator> streamedToolCalls)
	{
		streamedToolCalls.Clear();
		var position = 0;
		foreach (var item in toolCalls.OfType<JsonObject>())
		{
			var function = item["function"] as JsonObject;
			if (function is null)
				continue;

			var accumulator = new StreamingToolCallAccumulator
			{
				Id = item["id"]?.GetValue<string>() ?? $"call_{position}_{BuildArchitectureKey(function["name"]?.GetValue<string>() ?? "tool")}",
				Name = function["name"]?.GetValue<string>() ?? "tool"
			};
			var arguments = TryReadString(function["arguments"]);
			if (!string.IsNullOrWhiteSpace(arguments))
				accumulator.Arguments.Append(arguments);
			streamedToolCalls[position] = accumulator;
			position++;
		}
	}

	private JsonArray BuildStreamedOllamaToolCalls(IDictionary<int, StreamingToolCallAccumulator> streamedToolCalls)
	{
		var toolCalls = new JsonArray();
		foreach (var pair in streamedToolCalls.OrderBy(static item => item.Key))
		{
			var accumulator = pair.Value;
			var argumentsNode = TryParseJsonNode(accumulator.Arguments.ToString()) ?? JsonValue.Create(accumulator.Arguments.ToString());
			toolCalls.Add(new JsonObject
			{
				["id"] = string.IsNullOrWhiteSpace(accumulator.Id) ? $"call_{pair.Key}_{BuildArchitectureKey(accumulator.Name)}" : accumulator.Id,
				["type"] = "function",
				["function"] = new JsonObject
				{
					["index"] = pair.Key,
					["name"] = string.IsNullOrWhiteSpace(accumulator.Name) ? "tool" : accumulator.Name,
					["arguments"] = argumentsNode ?? new JsonObject()
				}
			});
		}

		return toolCalls;
	}

	private async Task CopyUpstreamResponseAsync(
		HttpListenerResponse response,
		HttpResponseMessage upstreamResponse,
		bool passthroughStreaming,
		CancellationToken ct)
	{
		response.StatusCode = (int)upstreamResponse.StatusCode;
		response.ContentType = upstreamResponse.Content.Headers.ContentType?.ToString() ?? "application/json";
		response.SendChunked = passthroughStreaming && IsEventStream(upstreamResponse);
		if (!response.SendChunked && upstreamResponse.Content.Headers.ContentLength is long contentLength)
			response.ContentLength64 = contentLength;

		foreach (var header in upstreamResponse.Headers)
			TryAddResponseHeader(response, header.Key, header.Value);
		foreach (var header in upstreamResponse.Content.Headers)
			TryAddResponseHeader(response, header.Key, header.Value);

		var output = response.OutputStream;
		await using var input = await upstreamResponse.Content.ReadAsStreamAsync(ct);
		await input.CopyToAsync(output, ct);
		await output.FlushAsync(ct);
		TryCloseResponse(response);
	}

	private static bool IsEventStream(HttpResponseMessage response) =>
		response.Content.Headers.ContentType?.MediaType?.Equals("text/event-stream", StringComparison.OrdinalIgnoreCase) == true;

	private static void TryAddResponseHeader(HttpListenerResponse response, string key, IEnumerable<string> values)
	{
		if (key.Equals("Transfer-Encoding", StringComparison.OrdinalIgnoreCase)
			|| key.Equals("Content-Length", StringComparison.OrdinalIgnoreCase)
			|| key.Equals("Content-Type", StringComparison.OrdinalIgnoreCase))
			return;

		try { response.Headers[key] = string.Join(",", values); } catch { }
	}

	private async Task<string> ExtractUpstreamErrorAsync(HttpStatusCode statusCode, string body)
	{
		if (string.IsNullOrWhiteSpace(body))
			return $"Upstream provider returned {(int)statusCode} {statusCode}.";

		try
		{
			var parsed = JsonNode.Parse(body);
			var message = parsed?["error"]?["message"]?.GetValue<string>()
				?? parsed?["error"]?.GetValue<string>()
				?? parsed?["message"]?.GetValue<string>();
			if (!string.IsNullOrWhiteSpace(message))
				return message;
		}
		catch
		{
		}

		return body;
	}

	private async Task<JsonObject> ReadRequestJsonAsync(HttpListenerRequest request, CancellationToken ct)
	{
		if (!request.HasEntityBody)
			throw new InvalidOperationException("The request body is required.");

		using var reader = new StreamReader(request.InputStream, request.ContentEncoding ?? Encoding.UTF8, leaveOpen: false);
		var body = await reader.ReadToEndAsync(ct);
		var json = JsonNode.Parse(body)?.AsObject();
		return json ?? throw new InvalidOperationException("The request body must be a JSON object.");
	}

	private async Task<JsonObject> BuildOllamaTagsResponseAsync(CancellationToken ct)
	{
		var models = new JsonArray();
		foreach (var model in await GetAdvertisedModelCatalogAsync(ct))
		{
			models.Add(new JsonObject
			{
				["name"] = model.LocalName,
				["model"] = model.LocalName,
				["modified_at"] = DateTimeOffset.UtcNow.ToString("O"),
				["size"] = 0,
				["digest"] = "trafficpilot",
				["capabilities"] = BuildOllamaCapabilities(model),
				["context_length"] = model.ContextLength,
				["multiplier"] = FormatMultiplier(model.Multiplier),
				["details"] = new JsonObject
				{
					["format"] = "trafficpilot",
					["family"] = model.Architecture,
					["families"] = new JsonArray(model.Architecture),
					["parameter_size"] = model.ParameterSize,
					["quantization_level"] = model.QuantizationLevel
				},
				["model_info"] = BuildOllamaModelInfo(model),
				["supports_tool_calling"] = model.SupportsToolCalling,
				["supports_vision"] = model.SupportsVision
			});
		}

		return new JsonObject { ["models"] = models };
	}

	private JsonObject BuildOllamaPsResponse()
	{
		var models = new JsonArray();
		foreach (var modelName in GetLoadedModelNames())
		{
			models.Add(new JsonObject
			{
				["name"] = modelName,
				["model"] = modelName,
				["size"] = 0,
				["digest"] = "trafficpilot",
				["details"] = new JsonObject
				{
					["parent_model"] = string.Empty,
					["format"] = "trafficpilot",
					["family"] = "forwarded",
					["families"] = new JsonArray("forwarded"),
					["parameter_size"] = "unknown",
					["quantization_level"] = "unknown"
				},
				["expires_at"] = DateTimeOffset.UtcNow.AddHours(1).ToString("O"),
				["size_vram"] = 0,
				["context_length"] = 100000
			});
		}

		return new JsonObject
		{
			["models"] = models
		};
	}

	private bool TryConvertOpenAiToolCalls(JsonObject parsed, out JsonArray toolCalls)
	{
		toolCalls = [];
		if (parsed["choices"]?[0]?["message"]?["tool_calls"] is not JsonArray sourceToolCalls)
			return false;

		toolCalls = ConvertOpenAiToolCallsToOllama(sourceToolCalls);
		return toolCalls.Count > 0;
	}

	private JsonArray ConvertOpenAiToolCallsToOllama(JsonArray sourceToolCalls)
	{
		var toolCalls = new JsonArray();

		var position = 0;
		foreach (var toolCall in sourceToolCalls.OfType<JsonObject>())
		{
			var function = toolCall["function"] as JsonObject;
			if (function is null)
				continue;

			var argumentsNode = function["arguments"] is JsonValue value
				&& value.TryGetValue<string>(out var argumentsText)
				&& TryParseJsonNode(argumentsText) is JsonNode parsedArguments
				? parsedArguments
				: function["arguments"]?.DeepClone() ?? new JsonObject();

			toolCalls.Add(new JsonObject
			{
				["id"] = toolCall["id"]?.DeepClone() ?? $"call_{position}_{BuildArchitectureKey(function["name"]?.GetValue<string>() ?? "tool")}",
				["type"] = "function",
				["function"] = new JsonObject
				{
					["index"] = position,
					["name"] = function["name"]?.GetValue<string>() ?? "tool",
					["arguments"] = argumentsNode
				}
			});
			position++;
		}

		return toolCalls;
	}

	private async Task<JsonObject> BuildOllamaShowResponseAsync(string modelName, CancellationToken ct)
	{
		var model = await GetAdvertisedModelCatalogEntryAsync(modelName, ct)
			?? CreateModelCatalogEntry(
				modelName,
				ResolveUpstreamChatModel(modelName),
				_settings.Provider.Name,
				DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
				isAlias: false,
				metadataSource: null);

		return new JsonObject
		{
			["modelfile"] = $"FROM {model.UpstreamModel}",
			["parameters"] = $"num_ctx {model.ContextLength}\nnum_predict {model.MaxOutputTokens}",
			["template"] = "{{ .Prompt }}",
			["license"] = $"Forwarded to {_settings.Provider.Name} via TrafficPilot.",
			["capabilities"] = BuildOllamaCapabilities(model),
			["modified_at"] = DateTimeOffset.UtcNow.ToString("O"),
			["multiplier"] = FormatMultiplier(model.Multiplier),
			["supports_tool_calling"] = model.SupportsToolCalling,
			["supports_vision"] = model.SupportsVision,
			["details"] = new JsonObject
			{
				["parent_model"] = model.UpstreamModel,
				["format"] = "trafficpilot",
				["family"] = model.Architecture,
				["families"] = new JsonArray(model.Architecture),
				["parameter_size"] = model.ParameterSize,
				["quantization_level"] = model.QuantizationLevel
			},
			["model_info"] = BuildOllamaModelInfo(model)
		};
	}

	private JsonObject BuildFoundryLocalStatusResponse()
	{
		var loopbackEndpoint = $"http://127.0.0.1:{_settings.FoundryPort}";
		var localhostEndpoint = $"http://localhost:{_settings.FoundryPort}";

		return new JsonObject
		{
			["Endpoints"] = new JsonArray(loopbackEndpoint, localhostEndpoint),
			["ModelDirPath"] = Path.Combine(
				Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
				"TrafficPilot",
				"RemoteModels"),
			["PipeName"] = "trafficpilot-foundry-local"
		};
	}

	private JsonArray BuildFoundryLocalModelsResponse(IEnumerable<string> modelNames)
	{
		return new JsonArray(modelNames
			.Where(static modelName => !string.IsNullOrWhiteSpace(modelName))
			.Select(static modelName => JsonValue.Create(modelName.Trim()))
			.ToArray());
	}

	private async Task<JsonArray> BuildFoundryLocalModelsResponseAsync(CancellationToken ct)
	{
		var modelNames = (await GetAdvertisedModelCatalogAsync(ct))
			.Select(static model => model.LocalName);
		return BuildFoundryLocalModelsResponse(modelNames);
	}

	private async Task<JsonObject> BuildFoundryLocalCatalogResponseAsync(CancellationToken ct)
	{
		var models = new JsonArray();
		foreach (var model in await GetAdvertisedModelCatalogAsync(ct))
		{
			var modelUri = BuildFoundryModelUri(model.LocalName);
			models.Add(new JsonObject
			{
				["name"] = model.LocalName,
				["displayName"] = model.LocalName,
				["providerType"] = "AzureFoundryLocal",
				["uri"] = modelUri,
				["publisher"] = _settings.Provider.Name,
				["task"] = "chat completion",
				["version"] = "1",
				["modelType"] = "OpenAI",
				["promptTemplate"] = new JsonObject
				{
					["system"] = "{{system}}",
					["user"] = "{{input}}",
					["assistant"] = "{{output}}",
					["prompt"] = "{{input}}"
				},
				["runtime"] = new JsonObject
				{
					["deviceType"] = "CPU",
					["executionProvider"] = "cpu"
				},
				["fileSizeMb"] = 0,
				["modelSettings"] = new JsonObject
				{
					["parameters"] = new JsonArray()
				},
				["alias"] = model.LocalName,
				["description"] = $"Forwarded to {_settings.Provider.Name} via TrafficPilot.",
				["supportsToolCalling"] = true,
				["license"] = "Remote",
				["licenseDescription"] = $"Remote model forwarded to {_settings.Provider.Name} via TrafficPilot.",
				["parentModelUri"] = modelUri,
				["maxOutputTokens"] = 8192,
				["minFLVersion"] = "0.0.0",
				["endpoints"] = new JsonArray
				{
					new JsonObject
					{
						["protocol"] = "OpenAI",
						["url"] = $"http://127.0.0.1:{_settings.FoundryPort}/v1",
						["model"] = model.LocalName
					}
				}
			});
		}

		return new JsonObject
		{
			["models"] = models
		};
	}

	private static string BuildFoundryModelUri(string modelName)
	{
		return $"azureml://registries/trafficpilot/models/{Uri.EscapeDataString(modelName)}/versions/1";
	}

	private async Task<JsonObject> BuildOpenAiModelsResponseAsync(CancellationToken ct)
	{
		var data = new JsonArray();
		foreach (var model in await GetAdvertisedModelCatalogAsync(ct))
		{
			var modelObj = new JsonObject
			{
				["id"] = model.LocalName,
				["object"] = "model",
				["created"] = model.CreatedUnixTime,
				["owned_by"] = string.IsNullOrWhiteSpace(model.OwnedBy) ? _settings.Provider.Name : model.OwnedBy
			};

			var capabilities = new JsonObject();
			if (model.SupportsToolCalling)
			{
				capabilities["tool_use"] = true;
				capabilities["function_calling"] = true;
			}
			if (model.SupportsVision)
				capabilities["vision"] = true;
			modelObj["capabilities"] = capabilities;

			data.Add(modelObj);
		}

		return new JsonObject
		{
			["object"] = "list",
			["data"] = data
		};
	}

	private async Task<IReadOnlyList<ModelCatalogEntry>> GetAdvertisedModelCatalogAsync(CancellationToken ct, bool forceRefresh = false)
	{
		var now = DateTimeOffset.UtcNow;
		if (!forceRefresh && _hasCachedModelCatalog && now < _modelCatalogExpiresAt)
			return _cachedModelCatalog;

		await _modelCatalogSyncLock.WaitAsync(ct);
		try
		{
			now = DateTimeOffset.UtcNow;
			if (!forceRefresh && _hasCachedModelCatalog && now < _modelCatalogExpiresAt)
				return _cachedModelCatalog;

			var configuredCatalog = BuildConfiguredModelCatalog();
			try
			{
				var upstreamCatalog = await FetchUpstreamModelCatalogAsync(ct);
				var mergedCatalog = MergeModelCatalogs(upstreamCatalog, configuredCatalog);
				CacheModelCatalog(mergedCatalog, now + ModelCatalogSuccessCacheDuration);
				LogInfo($"model catalog synced: upstream={upstreamCatalog.Count}, configured={configuredCatalog.Count}, advertised={mergedCatalog.Count}");
				return mergedCatalog;
			}
			catch (Exception ex)
			{
				if (_hasCachedModelCatalog)
				{
					_modelCatalogExpiresAt = now + ModelCatalogFailureCacheDuration;
					LogInfo($"model catalog sync failed: {ex.Message}; using cached catalog ({_cachedModelCatalog.Count} models)");
					return _cachedModelCatalog;
				}

				CacheModelCatalog(configuredCatalog, now + ModelCatalogFailureCacheDuration);
				LogInfo($"model catalog sync failed: {ex.Message}; using configured fallback ({configuredCatalog.Count} models)");
				return configuredCatalog;
			}
		}
		finally
		{
			_modelCatalogSyncLock.Release();
		}
	}

	private async Task<IReadOnlyList<ModelCatalogEntry>> FetchUpstreamModelCatalogAsync(CancellationToken ct)
	{
		var attempts = new List<string>();
		foreach (var candidate in EnumerateUpstreamModelCatalogRequests())
		{
			using var request = CreateUpstreamGetRequest(candidate.RequestUri);
			using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
			var responseBody = await response.Content.ReadAsStringAsync(ct);
			LogResponseIfEnabled(candidate.LogOperation, response.StatusCode, responseBody);

			if (!response.IsSuccessStatusCode)
			{
				attempts.Add($"{candidate.RequestUri} -> {(int)response.StatusCode} {response.ReasonPhrase}");
				continue;
			}

			var catalog = ParseUpstreamModelCatalog(responseBody);
			if (catalog.Count > 0)
				return catalog;

			attempts.Add($"{candidate.RequestUri} -> empty catalog");
		}

		throw new InvalidOperationException(
			$"Unable to load an upstream model catalog from {_settings.Provider.BaseUrl}. Tried: {string.Join("; ", attempts)}");
	}

	private IEnumerable<(Uri RequestUri, string LogOperation)> EnumerateUpstreamModelCatalogRequests()
	{
		var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

		bool TryAdd(Uri uri)
		{
			return seen.Add(uri.AbsoluteUri);
		}

		var baseUri = NormalizeBaseUri(_settings.Provider.BaseUrl);
		var basePath = baseUri.AbsolutePath.Trim('/');

		var primaryModelsUri = BuildUpstreamUri("models");
		if (TryAdd(primaryModelsUri))
			yield return (primaryModelsUri, "openai.models");

		if (!basePath.EndsWith("/v1", StringComparison.OrdinalIgnoreCase)
			&& !basePath.Equals("v1", StringComparison.OrdinalIgnoreCase))
		{
			var rootModelsUri = BuildUpstreamRootUri("v1/models");
			if (TryAdd(rootModelsUri))
				yield return (rootModelsUri, "openai.models");
		}

		var ollamaTagsUri = BuildUpstreamRootUri("api/tags");
		if (TryAdd(ollamaTagsUri))
			yield return (ollamaTagsUri, "ollama.tags");
	}

	private IReadOnlyList<ModelCatalogEntry> ParseUpstreamModelCatalog(string responseBody)
	{
		JsonNode? parsed;
		try
		{
			parsed = JsonNode.Parse(responseBody);
		}
		catch (JsonException ex)
		{
			throw new InvalidOperationException($"Upstream models payload was not valid JSON: {ex.Message}", ex);
		}

		if (parsed is null)
			throw new InvalidOperationException("Upstream models payload was empty.");

		var models = new Dictionary<string, ModelCatalogEntry>(StringComparer.OrdinalIgnoreCase);
		if (parsed is JsonObject parsedObject)
		{
			AddModelCatalogEntries(models, parsedObject["data"] as JsonArray);
			AddModelCatalogEntries(models, parsedObject["models"] as JsonArray);
		}
		else if (parsed is JsonArray parsedArray)
		{
			AddModelCatalogEntries(models, parsedArray);
		}

		if (models.Count == 0)
			throw new InvalidOperationException("Upstream models payload did not include any model IDs.");

		return models.Values
			.OrderBy(static model => model.LocalName, StringComparer.OrdinalIgnoreCase)
			.ToList();
	}

	private void AddModelCatalogEntries(
		IDictionary<string, ModelCatalogEntry> catalog,
		JsonArray? source)
	{
		if (source is null)
			return;

		foreach (var item in source)
		{
			var modelId = TryReadModelId(item);
			if (string.IsNullOrWhiteSpace(modelId))
				continue;

			var normalizedId = ResolveGeneratedUpstreamModelAliasOrSelf(modelId);
			var localName = BuildAdvertisedUpstreamLocalName(normalizedId, TryReadModelDisplayName(item, normalizedId));
			catalog[localName] = CreateModelCatalogEntry(
				localName,
				normalizedId,
				TryReadModelOwner(item) ?? _settings.Provider.Name,
				TryReadModelCreatedUnixTime(item) ?? DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
				isAlias: !localName.Equals(normalizedId, StringComparison.OrdinalIgnoreCase),
				item);
		}
	}

	private IReadOnlyList<ModelCatalogEntry> BuildConfiguredModelCatalog()
	{
		var models = new Dictionary<string, ModelCatalogEntry>(StringComparer.OrdinalIgnoreCase);
		var createdUnixTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

		foreach (var mapping in _settings.ModelMappings)
		{
			if (string.IsNullOrWhiteSpace(mapping.LocalModel))
				continue;

			var localModel = mapping.LocalModel.Trim();
			var upstreamModel = string.IsNullOrWhiteSpace(mapping.UpstreamModel)
				? localModel
				: mapping.UpstreamModel.Trim();
			models[localModel] = CreateModelCatalogEntry(
				localModel,
				upstreamModel,
				_settings.Provider.Name,
				createdUnixTime,
				isAlias: !localModel.Equals(upstreamModel, StringComparison.OrdinalIgnoreCase),
				metadataSource: null);
		}

		AddConfiguredModelCatalogEntry(models, _settings.Provider.DefaultModel, createdUnixTime);
		AddConfiguredModelCatalogEntry(models, _settings.Provider.DefaultEmbeddingModel, createdUnixTime);

		return models.Values
			.OrderBy(static model => model.LocalName, StringComparer.OrdinalIgnoreCase)
			.ToList();
	}

	private void AddConfiguredModelCatalogEntry(
		IDictionary<string, ModelCatalogEntry> catalog,
		string? modelName,
		long createdUnixTime)
	{
		if (string.IsNullOrWhiteSpace(modelName))
			return;

		var normalizedModel = modelName.Trim();
		var upstreamModel = ResolveGeneratedUpstreamModelAliasOrSelf(normalizedModel);
		var localName = BuildAdvertisedUpstreamLocalName(upstreamModel);
		catalog[localName] = CreateModelCatalogEntry(
			localName,
			upstreamModel,
			_settings.Provider.Name,
			createdUnixTime,
			isAlias: !localName.Equals(upstreamModel, StringComparison.OrdinalIgnoreCase),
			metadataSource: null);
	}

	private IReadOnlyList<string> GetConfiguredLocalModelNames() =>
		BuildConfiguredModelCatalog()
			.Select(static model => model.LocalName)
			.ToList();

	private static IReadOnlyList<ModelCatalogEntry> MergeModelCatalogs(
		IReadOnlyList<ModelCatalogEntry> upstreamCatalog,
		IReadOnlyList<ModelCatalogEntry> configuredCatalog)
	{
		var merged = new Dictionary<string, ModelCatalogEntry>(StringComparer.OrdinalIgnoreCase);
		foreach (var model in upstreamCatalog)
			merged[model.LocalName] = model;
		foreach (var model in configuredCatalog)
		{
			if (merged.ContainsKey(model.LocalName))
				continue;

			var upstreamMatch = upstreamCatalog.FirstOrDefault(upstream =>
				upstream.LocalName.Equals(model.UpstreamModel, StringComparison.OrdinalIgnoreCase));
			if (upstreamMatch is not null)
			{
				merged[model.LocalName] = upstreamMatch with
				{
					LocalName = model.LocalName,
					UpstreamModel = model.UpstreamModel,
					OwnedBy = model.OwnedBy,
					CreatedUnixTime = model.CreatedUnixTime,
					IsAlias = model.IsAlias
				};
				continue;
			}

			merged[model.LocalName] = model;
		}

		return merged.Values
			.OrderBy(static model => model.LocalName, StringComparer.OrdinalIgnoreCase)
			.ToList();
	}

	private async Task<ModelCatalogEntry?> GetAdvertisedModelCatalogEntryAsync(string modelName, CancellationToken ct)
	{
		if (string.IsNullOrWhiteSpace(modelName))
			return null;

		var catalog = await GetAdvertisedModelCatalogAsync(ct);
		return catalog.FirstOrDefault(model =>
			model.LocalName.Equals(modelName, StringComparison.OrdinalIgnoreCase));
	}

	private ModelCatalogEntry CreateModelCatalogEntry(
		string localName,
		string upstreamModel,
		string ownedBy,
		long createdUnixTime,
		bool isAlias,
		JsonNode? metadataSource)
	{
		var inferenceModelName = string.IsNullOrWhiteSpace(upstreamModel) ? localName : upstreamModel;
		var supportsEmbeddings = InferEmbeddingSupport(inferenceModelName, metadataSource);
		var supportsToolCalling = !supportsEmbeddings
			&& !IsAnthropicProvider
			&& InferToolSupport(inferenceModelName, metadataSource);
		var supportsVision = !supportsEmbeddings
			&& !IsAnthropicProvider
			&& InferVisionSupport(inferenceModelName, metadataSource);
		var multiplier = TryReadDoubleFromAny(metadataSource,
			"multiplier",
			"rateMultiplier",
			"rate_multiplier",
			"costMultiplier",
			"cost_multiplier",
			"usageMultiplier",
			"usage_multiplier");

		return new ModelCatalogEntry(
			localName,
			upstreamModel,
			ownedBy,
			createdUnixTime,
			isAlias,
			InferArchitecture(inferenceModelName, metadataSource),
			TryReadLongFromAny(metadataSource,
				"contextWindowTokens",
				"context_window_tokens",
				"contextWindow",
				"context_window",
				"contextLength",
				"context_length",
				"maxInputTokens",
				"max_input_tokens",
				"inputTokenLimit",
				"input_token_limit")
				?? InferContextLengthFromModelName(inferenceModelName),
			TryReadLongFromAny(metadataSource,
				"maxOutputTokens",
				"max_output_tokens",
				"outputTokenLimit",
				"output_token_limit",
				"maxCompletionTokens",
				"max_completion_tokens")
				?? DefaultMaxOutputTokens,
			supportsToolCalling,
			supportsVision,
			supportsEmbeddings,
			InferThinkingSupport(inferenceModelName, metadataSource),
			TryReadStringFromAny(metadataSource, "parameter_size", "parameterSize") ?? (supportsEmbeddings ? "embedding" : "unknown"),
			TryReadStringFromAny(metadataSource, "quantization_level", "quantizationLevel") ?? "remote",
			TryReadLongFromAny(metadataSource, "general.parameter_count", "parameter_count", "parameterCount"),
			multiplier);
	}

	private static JsonArray BuildOllamaCapabilities(ModelCatalogEntry model)
	{
		var capabilities = new List<string>();
		if (!model.SupportsEmbeddings)
		{
			capabilities.Add("completion");
			capabilities.Add("chat");
		}

		if (model.SupportsToolCalling)
			capabilities.Add("tools");
		if (model.SupportsVision)
			capabilities.Add("vision");
		if (model.SupportsEmbeddings)
			capabilities.Add("embedding");
		if (model.SupportsThinking)
			capabilities.Add("thinking");

		return new JsonArray(capabilities
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.Select(static capability => JsonValue.Create(capability))
			.ToArray());
	}

	private JsonObject BuildOllamaModelInfo(ModelCatalogEntry model)
	{
		var modelInfo = new JsonObject
		{
			["general.architecture"] = model.Architecture,
			[$"{model.Architecture}.context_length"] = model.ContextLength,
			["trafficpilot.context_length"] = model.ContextLength,
			["trafficpilot.max_output_tokens"] = model.MaxOutputTokens,
			["trafficpilot.provider_name"] = _settings.Provider.Name,
			["trafficpilot.provider_base_url"] = _settings.Provider.BaseUrl,
			["trafficpilot.upstream_model"] = model.UpstreamModel,
			["trafficpilot.supports_tool_calling"] = model.SupportsToolCalling,
			["trafficpilot.supports_vision"] = model.SupportsVision
		};

		if (model.ParameterCount is not null)
			modelInfo["general.parameter_count"] = model.ParameterCount.Value;
		if (model.SupportsVision)
			modelInfo[$"{model.Architecture}.vision.enabled"] = true;
		if (model.SupportsThinking)
			modelInfo[$"{model.Architecture}.thinking.enabled"] = true;
		if (model.Multiplier is not null)
			modelInfo["trafficpilot.multiplier"] = model.Multiplier.Value;

		return modelInfo;
	}

	private static string? FormatMultiplier(double? multiplier)
	{
		if (multiplier is null)
			return null;

		return $"{multiplier.Value.ToString("0.##", CultureInfo.InvariantCulture)}x";
	}

	private static string InferArchitecture(string modelName, JsonNode? metadataSource)
	{
		return TryReadStringFromAny(metadataSource,
				"general.architecture",
				"architecture",
				"family")
			?? BuildArchitectureKey(modelName);
	}

	private static bool InferToolSupport(string modelName, JsonNode? metadataSource)
	{
		var explicitValue = TryReadBoolFromAny(metadataSource,
			"supportsToolCalling",
			"supports_tool_calling",
			"supportsTools",
			"supports_tools",
			"supportsFunctionCalling",
			"supports_function_calling");
		if (explicitValue is not null)
			return explicitValue.Value;

		if (ContainsAnyValue(FindPropertyValue(metadataSource,
				"capabilities",
				"supportedCapabilities",
				"supported_capabilities",
				"features",
				"supportedFeatures",
				"supported_features"),
			"tools",
			"tool",
			"tool_calling",
			"function_calling"))
			return true;

		return IsLikelyToolCapableModel(modelName);
	}

	private static bool InferVisionSupport(string modelName, JsonNode? metadataSource)
	{
		var explicitValue = TryReadBoolFromAny(metadataSource,
			"supportsVision",
			"supports_vision",
			"supportsImageInput",
			"supports_image_input",
			"vision");
		if (explicitValue is not null)
			return explicitValue.Value;

		if (ContainsAnyValue(FindPropertyValue(metadataSource,
				"capabilities",
				"supportedCapabilities",
				"supported_capabilities",
				"inputModalities",
				"input_modalities",
				"modalities"),
			"vision",
			"image",
			"images",
			"multimodal"))
			return true;

		return IsLikelyVisionModel(modelName);
	}

	private static bool InferEmbeddingSupport(string modelName, JsonNode? metadataSource)
	{
		var explicitValue = TryReadBoolFromAny(metadataSource,
			"supportsEmbeddings",
			"supports_embeddings",
			"embedding");
		if (explicitValue is not null)
			return explicitValue.Value;

		if (ContainsAnyValue(FindPropertyValue(metadataSource,
				"capabilities",
				"supportedCapabilities",
				"supported_capabilities",
				"modelType",
				"model_type",
				"task"),
			"embedding",
			"embeddings"))
			return true;

		return IsLikelyEmbeddingModel(modelName);
	}

	private static bool InferThinkingSupport(string modelName, JsonNode? metadataSource)
	{
		var explicitValue = TryReadBoolFromAny(metadataSource,
			"supportsThinking",
			"supports_thinking",
			"thinking");
		if (explicitValue is not null)
			return explicitValue.Value;

		if (ContainsAnyValue(FindPropertyValue(metadataSource,
				"capabilities",
				"supportedCapabilities",
				"supported_capabilities",
				"features"),
			"thinking",
			"reasoning"))
			return true;

		var normalized = modelName.Trim().ToLowerInvariant();
		return normalized.StartsWith("o1", StringComparison.Ordinal)
			|| normalized.StartsWith("o3", StringComparison.Ordinal)
			|| normalized.StartsWith("o4", StringComparison.Ordinal)
			|| normalized.StartsWith("gpt-5", StringComparison.Ordinal)
			|| normalized.Contains("reason", StringComparison.Ordinal);
	}

	private static bool IsLikelyEmbeddingModel(string modelName)
	{
		var normalized = modelName.Trim().ToLowerInvariant();
		return normalized.Contains("embedding", StringComparison.Ordinal)
			|| normalized.Contains("embed", StringComparison.Ordinal)
			|| normalized.Contains("text-embedding", StringComparison.Ordinal);
	}

	private static bool IsLikelyVisionModel(string modelName)
	{
		var normalized = modelName.Trim().ToLowerInvariant();
		return normalized.StartsWith("gpt-4o", StringComparison.Ordinal)
			|| normalized.StartsWith("gpt-4.1", StringComparison.Ordinal)
			|| normalized.StartsWith("gpt-5", StringComparison.Ordinal)
			|| normalized.StartsWith("claude-3", StringComparison.Ordinal)
			|| normalized.StartsWith("claude-4", StringComparison.Ordinal)
			|| normalized.StartsWith("gemini", StringComparison.Ordinal)
			|| normalized.Contains("vision", StringComparison.Ordinal)
			|| normalized.Contains("-vl", StringComparison.Ordinal)
			|| normalized.Contains("multimodal", StringComparison.Ordinal)
			|| normalized.Contains("llava", StringComparison.Ordinal)
			|| normalized.Contains("pixtral", StringComparison.Ordinal)
			|| normalized.Contains("minicpm-v", StringComparison.Ordinal)
			|| normalized.Contains("moondream", StringComparison.Ordinal)
			|| normalized.Contains("internvl", StringComparison.Ordinal);
	}

	private static bool IsLikelyToolCapableModel(string modelName)
	{
		if (IsLikelyEmbeddingModel(modelName))
			return false;

		var normalized = modelName.Trim().ToLowerInvariant();
		return normalized.StartsWith("gpt-", StringComparison.Ordinal)
			|| normalized.StartsWith("o1", StringComparison.Ordinal)
			|| normalized.StartsWith("o3", StringComparison.Ordinal)
			|| normalized.StartsWith("o4", StringComparison.Ordinal)
			|| normalized.StartsWith("claude", StringComparison.Ordinal)
			|| normalized.StartsWith("gemini", StringComparison.Ordinal)
			|| normalized.StartsWith("grok", StringComparison.Ordinal)
			|| normalized.StartsWith("llama", StringComparison.Ordinal)
			|| normalized.StartsWith("qwen", StringComparison.Ordinal)
			|| normalized.StartsWith("deepseek", StringComparison.Ordinal)
			|| normalized.StartsWith("mistral", StringComparison.Ordinal)
			|| normalized.StartsWith("codestral", StringComparison.Ordinal)
			|| normalized.StartsWith("kimi", StringComparison.Ordinal)
			|| normalized.Contains("codex", StringComparison.Ordinal)
			|| normalized.Contains("tool", StringComparison.Ordinal);
	}

	private static long InferContextLengthFromModelName(string modelName)
	{
		var normalized = modelName.Trim().ToLowerInvariant();
		if (normalized.StartsWith("gpt-5", StringComparison.Ordinal))
			return 400_000;
		if (normalized.StartsWith("gpt-4.1", StringComparison.Ordinal))
			return 1_000_000;
		if (normalized.StartsWith("gpt-4o", StringComparison.Ordinal))
			return 128_000;
		if (normalized.StartsWith("claude", StringComparison.Ordinal))
			return 200_000;
		if (normalized.StartsWith("gemini", StringComparison.Ordinal))
			return 1_000_000;

		return DefaultContextLength;
	}

	private static string BuildArchitectureKey(string modelName)
	{
		if (string.IsNullOrWhiteSpace(modelName))
			return "forwarded";

		var builder = new StringBuilder(modelName.Length);
		var previousWasSeparator = false;
		foreach (var ch in modelName.Trim().ToLowerInvariant())
		{
			if (char.IsLetterOrDigit(ch))
			{
				builder.Append(ch);
				previousWasSeparator = false;
				continue;
			}

			if (previousWasSeparator)
				continue;

			builder.Append('_');
			previousWasSeparator = true;
		}

		var normalized = builder.ToString().Trim('_');
		return normalized.Length == 0 ? "forwarded" : normalized;
	}

	private static string BuildProviderModelAlias(string? providerName, string? baseUrl)
	{
		if (Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri))
		{
			var host = uri.IdnHost.Trim().TrimEnd('.').ToLowerInvariant();
			if (!string.IsNullOrWhiteSpace(host))
				return host.Replace(".", string.Empty, StringComparison.Ordinal);
		}

		var alias = BuildCompactAlias(providerName);
		if (!string.IsNullOrWhiteSpace(alias))
			return alias;

		return "up";
	}

	private static IEnumerable<string> GetProviderModelAliasCandidates(string? providerName, string? baseUrl)
	{
		var primary = BuildProviderModelAlias(providerName, baseUrl);
		if (!string.IsNullOrWhiteSpace(primary))
			yield return primary;

		if (Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri))
		{
			var host = uri.IdnHost.Trim().TrimEnd('.').ToLowerInvariant();
			if (!string.IsNullOrWhiteSpace(host)
				&& !host.Equals(primary, StringComparison.OrdinalIgnoreCase))
			{
				yield return host;
			}
		}
	}

	private string? TryResolveCatalogMappedUpstreamModel(string localModel)
	{
		if (string.IsNullOrWhiteSpace(localModel))
			return null;

		var cachedMatch = _cachedModelCatalog.FirstOrDefault(model =>
			model.LocalName.Equals(localModel, StringComparison.OrdinalIgnoreCase));
		if (cachedMatch is not null && !string.IsNullOrWhiteSpace(cachedMatch.UpstreamModel))
			return cachedMatch.UpstreamModel;

		// Check model mappings directly instead of calling BuildConfiguredModelCatalog(),
		// which would cause infinite recursion via AddConfiguredModelCatalogEntry →
		// ResolveGeneratedUpstreamModelAliasOrSelf → TryResolveCatalogMappedUpstreamModel.
		var mappingMatch = _settings.ModelMappings.FirstOrDefault(m =>
			m.LocalModel.Equals(localModel, StringComparison.OrdinalIgnoreCase)
			&& !string.IsNullOrWhiteSpace(m.UpstreamModel));
		if (mappingMatch is not null)
			return mappingMatch.UpstreamModel.Trim();

		return null;
	}

	private static string BuildCompactAlias(string? source)
	{
		if (string.IsNullOrWhiteSpace(source))
			return string.Empty;

		var tokens = new List<string>();
		var current = new StringBuilder();
		Rune? previousRune = null;

		foreach (var rune in source.EnumerateRunes())
		{
			if (!Rune.IsLetterOrDigit(rune))
			{
				FlushAliasToken(tokens, current);
				previousRune = null;
				continue;
			}

			if (previousRune is not null
				&& Rune.IsUpper(rune)
				&& !Rune.IsUpper(previousRune.Value)
				&& current.Length > 0)
			{
				FlushAliasToken(tokens, current);
			}

			current.Append(rune.ToString().ToLowerInvariant());
			previousRune = rune;
		}

		FlushAliasToken(tokens, current);

		if (tokens.Count == 0)
			return string.Empty;

		if (tokens.Count == 1)
		{
			var token = tokens[0];
			return token.Length <= 4 ? token : token[..4];
		}

		var alias = new StringBuilder(4);
		foreach (var token in tokens)
		{
			if (token.Length == 0)
				continue;

			alias.Append(token[0]);
			if (alias.Length >= 4)
				break;
		}

		return alias.ToString();
	}

	private static void FlushAliasToken(List<string> tokens, StringBuilder current)
	{
		if (current.Length == 0)
			return;

		tokens.Add(current.ToString());
		current.Clear();
	}

	private static JsonNode? FindPropertyValue(JsonNode? node, params string[] propertyNames)
	{
		if (node is null || propertyNames.Length == 0)
			return null;

		var queue = new Queue<JsonNode>();
		queue.Enqueue(node);
		while (queue.Count > 0)
		{
			var current = queue.Dequeue();
			if (current is not JsonObject obj)
				continue;

			foreach (var property in obj)
			{
				if (property.Value is null)
					continue;

				if (propertyNames.Any(name => property.Key.Equals(name, StringComparison.OrdinalIgnoreCase)))
					return property.Value;

				if (property.Value is JsonObject nestedObject)
					queue.Enqueue(nestedObject);
			}
		}

		return null;
	}

	private static bool ContainsAnyValue(JsonNode? node, params string[] candidates)
	{
		if (node is null || candidates.Length == 0)
			return false;

		var set = new HashSet<string>(candidates, StringComparer.OrdinalIgnoreCase);
		return EnumerateStringValues(node).Any(value => set.Contains(value));
	}

	private static IEnumerable<string> EnumerateStringValues(JsonNode? node)
	{
		if (node is null)
			yield break;

		if (node is JsonValue value)
		{
			if (value.TryGetValue<string>(out var stringValue) && !string.IsNullOrWhiteSpace(stringValue))
				yield return stringValue.Trim();
			yield break;
		}

		if (node is JsonArray array)
		{
			foreach (var item in array)
			{
				foreach (var stringValue in EnumerateStringValues(item))
					yield return stringValue;
			}
			yield break;
		}

		if (node is JsonObject obj)
		{
			foreach (var property in obj)
			{
				foreach (var stringValue in EnumerateStringValues(property.Value))
					yield return stringValue;
			}
		}
	}

	private static string? TryReadStringFromAny(JsonNode? node, params string[] propertyNames) =>
		TryReadString(FindPropertyValue(node, propertyNames));

	private static bool? TryReadBoolFromAny(JsonNode? node, params string[] propertyNames) =>
		TryReadBool(FindPropertyValue(node, propertyNames));

	private static double? TryReadDoubleFromAny(JsonNode? node, params string[] propertyNames) =>
		TryReadDouble(FindPropertyValue(node, propertyNames));

	private static long? TryReadLongFromAny(JsonNode? node, params string[] propertyNames) =>
		TryReadInt64(FindPropertyValue(node, propertyNames));

	private void CacheModelCatalog(IReadOnlyList<ModelCatalogEntry> catalog, DateTimeOffset expiresAt)
	{
		_cachedModelCatalog = catalog;
		_hasCachedModelCatalog = true;
		_modelCatalogExpiresAt = expiresAt;
	}

	private async Task<bool> ModelExistsAsync(string modelName, CancellationToken ct)
	{
		if (string.IsNullOrWhiteSpace(modelName))
			return false;

		var catalog = await GetAdvertisedModelCatalogAsync(ct);
		if (catalog.Any(model => model.LocalName.Equals(modelName, StringComparison.OrdinalIgnoreCase)))
			return true;

		catalog = await GetAdvertisedModelCatalogAsync(ct, forceRefresh: true);
		return catalog.Any(model => model.LocalName.Equals(modelName, StringComparison.OrdinalIgnoreCase));
	}

	private static string? TryReadModelId(JsonNode? node)
	{
		if (node is null)
			return null;

		if (node is JsonObject obj)
		{
			return TryReadString(obj["id"])
				?? TryReadString(obj["name"])
				?? TryReadString(obj["model"]);
		}

		return TryReadString(node);
	}

	private static string? TryReadModelOwner(JsonNode? node)
	{
		if (node is not JsonObject obj)
			return null;

		return TryReadString(obj["owned_by"])
			?? TryReadString(obj["organization"])
			?? TryReadString(obj["publisher"]);
	}

	private static string? TryReadModelDisplayName(JsonNode? node, string? modelId)
	{
		if (node is not JsonObject obj)
			return null;

		var displayName = TryReadString(obj["display_name"])
			?? TryReadString(obj["displayName"])
			?? TryReadString(obj["title"])
			?? TryReadString(obj["label"]);
		if (!string.IsNullOrWhiteSpace(displayName))
			return displayName;

		var alternateName = TryReadString(obj["name"]);
		if (!string.IsNullOrWhiteSpace(alternateName)
			&& !string.Equals(alternateName.Trim(), modelId?.Trim(), StringComparison.OrdinalIgnoreCase))
		{
			return alternateName;
		}

		return null;
	}

	private static long? TryReadModelCreatedUnixTime(JsonNode? node)
	{
		if (node is not JsonObject obj)
			return null;

		return TryReadInt64(obj["created"])
			?? TryReadInt64(obj["created_at"]);
	}

	private static string? TryReadString(JsonNode? node)
	{
		if (node is null)
			return null;

		try
		{
			return node.GetValue<string>();
		}
		catch
		{
			return null;
		}
	}

	private static long? TryReadInt64(JsonNode? node)
	{
		if (node is null)
			return null;

		try
		{
			return node.GetValue<long>();
		}
		catch
		{
		}

		try
		{
			return node.GetValue<int>();
		}
		catch
		{
		}

		var stringValue = TryReadString(node);
		if (long.TryParse(stringValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
			return parsed;

		return TryParseScaledLong(stringValue);
	}

	private static bool? TryReadBool(JsonNode? node)
	{
		if (node is null)
			return null;

		try
		{
			return node.GetValue<bool>();
		}
		catch
		{
		}

		var stringValue = TryReadString(node);
		return bool.TryParse(stringValue, out var parsed) ? parsed : null;
	}

	private static double? TryReadDouble(JsonNode? node)
	{
		if (node is null)
			return null;

		try
		{
			return node.GetValue<double>();
		}
		catch
		{
		}

		try
		{
			return node.GetValue<float>();
		}
		catch
		{
		}

		var stringValue = TryReadString(node);
		if (string.IsNullOrWhiteSpace(stringValue))
			return null;

		var normalized = stringValue.Trim();
		if (normalized.EndsWith("x", StringComparison.OrdinalIgnoreCase))
			normalized = normalized[..^1];

		return double.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed)
			? parsed
			: null;
	}

	private static long? TryParseScaledLong(string? value)
	{
		if (string.IsNullOrWhiteSpace(value))
			return null;

		var normalized = value.Trim().Replace(",", string.Empty, StringComparison.Ordinal);
		var multiplier = 1d;
		if (normalized.EndsWith("k", StringComparison.OrdinalIgnoreCase))
		{
			multiplier = 1_000d;
			normalized = normalized[..^1];
		}
		else if (normalized.EndsWith("m", StringComparison.OrdinalIgnoreCase))
		{
			multiplier = 1_000_000d;
			normalized = normalized[..^1];
		}
		else if (normalized.EndsWith("b", StringComparison.OrdinalIgnoreCase))
		{
			multiplier = 1_000_000_000d;
			normalized = normalized[..^1];
		}

		if (!double.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed))
			return null;

		return (long)Math.Round(parsed * multiplier, MidpointRounding.AwayFromZero);
	}

	private IReadOnlyList<string> GetLoadedModelNames()
	{
		lock (_loadedModelsLock)
		{
			if (_loadedModels.Count == 0)
				return [];

			return _loadedModels
				.OrderBy(static model => model, StringComparer.OrdinalIgnoreCase)
				.ToList();
		}
	}

	private void MarkModelLoaded(string modelName)
	{
		lock (_loadedModelsLock)
			_loadedModels.Add(modelName);
	}

	private void MarkModelUnloaded(string modelName)
	{
		lock (_loadedModelsLock)
			_loadedModels.RemoveWhere(existing => existing.Equals(modelName, StringComparison.OrdinalIgnoreCase));
	}

	private void ClearLoadedModels()
	{
		lock (_loadedModelsLock)
			_loadedModels.Clear();
	}

	private static bool TryExtractModelName(string path, string prefix, out string modelName)
	{
		modelName = string.Empty;
		if (!path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
			return false;

		var encodedName = path[prefix.Length..].Trim();
		if (encodedName.Length == 0)
			return false;

		modelName = Uri.UnescapeDataString(encodedName);
		return modelName.Length > 0;
	}

	private async Task WriteErrorAsync(
		HttpListenerResponse response,
		string localPath,
		HttpStatusCode statusCode,
		string message,
		string responseStyle,
		HttpStatusCode? upstreamStatus,
		string? upstreamBody,
		string? localModel)
	{
		JsonNode payload;
		if (responseStyle.Equals("ollama", StringComparison.OrdinalIgnoreCase))
		{
			var ollamaError = new JsonObject
			{
				["error"] = message
			};

			if (_settings.IncludeErrorDiagnostics)
				ollamaError["diagnostics"] = BuildDiagnostics(localPath, upstreamStatus, upstreamBody, localModel);

			payload = ollamaError;
		}
		else
		{
			var errorObject = new JsonObject
			{
				["message"] = message,
				["type"] = "trafficpilot_local_forwarder_error"
			};

			if (_settings.IncludeErrorDiagnostics)
				errorObject["details"] = BuildDiagnostics(localPath, upstreamStatus, upstreamBody, localModel);

			payload = new JsonObject
			{
				["error"] = errorObject
			};
		}

		LogInfo($"error {localPath}: {(int)statusCode} {statusCode} | {message}");
		await WriteJsonAsync(response, statusCode, payload);
	}

	private JsonObject BuildDiagnostics(string localPath, HttpStatusCode? upstreamStatus, string? upstreamBody, string? localModel)
	{
		var diagnostics = new JsonObject
		{
			["local_path"] = localPath,
			["provider_protocol"] = _settings.Provider.Protocol,
			["provider_name"] = _settings.Provider.Name,
			["provider_base_url"] = _settings.Provider.BaseUrl,
			["chat_endpoint"] = _settings.Provider.ChatEndpoint,
			["embeddings_endpoint"] = _settings.Provider.EmbeddingsEndpoint
		};

		if (!string.IsNullOrWhiteSpace(localModel))
			diagnostics["local_model"] = localModel;
		if (upstreamStatus is not null)
			diagnostics["upstream_status"] = $"{(int)upstreamStatus.Value} {upstreamStatus.Value}";
		if (!string.IsNullOrWhiteSpace(upstreamBody))
			diagnostics["upstream_body_preview"] = TruncateForLog(upstreamBody);

		return diagnostics;
	}

	private static bool HttpMethodsEqual(string? actual, string expected) =>
		string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase);

	private static async Task WriteJsonAsync(HttpListenerResponse response, HttpStatusCode statusCode, JsonNode payload, string contentType = "application/json")
	{
		response.StatusCode = (int)statusCode;
		response.ContentType = contentType;
		var json = payload.ToJsonString(JsonOptions);
		var bytes = Encoding.UTF8.GetBytes(json);
		response.ContentLength64 = bytes.Length;
		await response.OutputStream.WriteAsync(bytes);
		await response.OutputStream.FlushAsync();
		TryCloseResponse(response);
	}

	private static async Task WriteJsonAsync(HttpListenerResponse response, HttpStatusCode statusCode, string json, string contentType = "application/json")
	{
		response.StatusCode = (int)statusCode;
		response.ContentType = contentType;
		var bytes = Encoding.UTF8.GetBytes(json);
		response.ContentLength64 = bytes.Length;
		await response.OutputStream.WriteAsync(bytes);
		await response.OutputStream.FlushAsync();
		TryCloseResponse(response);
	}

	private static async Task WritePlainTextAsync(HttpListenerResponse response, HttpStatusCode statusCode, string text, string contentType = "text/plain")
	{
		response.StatusCode = (int)statusCode;
		response.ContentType = contentType;
		var bytes = Encoding.UTF8.GetBytes(text);
		response.ContentLength64 = bytes.Length;
		await response.OutputStream.WriteAsync(bytes);
		await response.OutputStream.FlushAsync();
		TryCloseResponse(response);
	}

	private static Task WriteEmptyAsync(HttpListenerResponse response, HttpStatusCode statusCode)
	{
		response.StatusCode = (int)statusCode;
		response.ContentLength64 = 0;
		TryCloseResponse(response);
		return Task.CompletedTask;
	}

	private static void TryCloseResponse(HttpListenerResponse response)
	{
		try { response.Close(); } catch { }
	}

	private void LogInfo(string message) => OnLog?.Invoke($"[Local API] {message}");

	private static string FormatSettingValue(string? value)
	{
		return string.IsNullOrWhiteSpace(value)
			? "<empty>"
			: value.Trim();
	}
}
