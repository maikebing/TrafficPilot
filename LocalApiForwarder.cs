using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace TrafficPilot;

internal sealed class LocalApiForwarder : IDisposable
{
	private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

	private readonly LocalApiForwarderSettings _settings;
	private readonly string _apiKey;
	private readonly HttpClient _httpClient;
	private readonly CancellationTokenSource _cts = new();
	private readonly List<HttpListener> _listeners = [];
	private readonly List<Task> _acceptLoops = [];

	public event Action<string>? OnLog;

	public LocalApiForwarder(LocalApiForwarderSettings settings, string? apiKey)
	{
		_settings = settings ?? throw new ArgumentNullException(nameof(settings));
		_apiKey = apiKey?.Trim() ?? string.Empty;
		_httpClient = new HttpClient
		{
			Timeout = Timeout.InfiniteTimeSpan
		};
	}

	public Task StartAsync()
	{
		if (!_settings.Enabled)
			return Task.CompletedTask;

		ValidateSettings(_settings);

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
		try
		{
			if (HttpMethodsEqual(context.Request.HttpMethod, "GET") && path.Equals("/api/tags", StringComparison.OrdinalIgnoreCase))
			{
				await WriteJsonAsync(context.Response, HttpStatusCode.OK, BuildOllamaTagsResponse());
				return;
			}

			if (HttpMethodsEqual(context.Request.HttpMethod, "GET") && path.Equals("/api/version", StringComparison.OrdinalIgnoreCase))
			{
				await WriteJsonAsync(context.Response, HttpStatusCode.OK, new JsonObject
				{
					["version"] = "trafficpilot-local-forwarder"
				});
				return;
			}

			if (HttpMethodsEqual(context.Request.HttpMethod, "GET")
				&& (path.Equals("/v1/models", StringComparison.OrdinalIgnoreCase)
					|| path.Equals("/models", StringComparison.OrdinalIgnoreCase)))
			{
				await WriteJsonAsync(context.Response, HttpStatusCode.OK, BuildOpenAiModelsResponse());
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
			LogInfo($"Local API request error on {path}: {ex.Message}");
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
		if (stream)
		{
			await WriteErrorAsync(context.Response, "/v1/responses", HttpStatusCode.BadRequest,
				"Streaming responses are not supported by the local adapter yet.",
				"openai", null, null, null);
			return;
		}

		if (IsAnthropicProvider)
		{
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

		var chatRequest = BuildOpenAiChatRequestFromResponses(requestJson);
		using var request = CreateUpstreamRequest(_settings.Provider.ChatEndpoint, chatRequest);
		using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
		var responseBody = await response.Content.ReadAsStringAsync(ct);
		LogResponseIfEnabled("openai.responses", response.StatusCode, responseBody);

		if (!response.IsSuccessStatusCode)
		{
			await WriteErrorAsync(context.Response, "/v1/responses", response.StatusCode,
				await ExtractUpstreamErrorAsync(response.StatusCode, responseBody),
				"openai", response.StatusCode, responseBody, null);
			return;
		}

		var upstreamJsonResponse = ParseJsonObject(responseBody, "OpenAI chat response");
		var localResponse = ConvertOpenAiChatResponseToResponses(upstreamJsonResponse, requestedModel);
		await WriteJsonAsync(context.Response, HttpStatusCode.OK, localResponse);
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

	private HttpRequestMessage CreateUpstreamRequest(string relativePath, JsonNode payload)
	{
		var requestUri = BuildUpstreamUri(relativePath);
		var request = new HttpRequestMessage(HttpMethod.Post, requestUri)
		{
			Content = new StringContent(payload.ToJsonString(JsonOptions), Encoding.UTF8, "application/json")
		};

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

	private string ResolveUpstreamChatModel(string? localModel)
	{
		if (!string.IsNullOrWhiteSpace(localModel))
		{
			var mapping = _settings.ModelMappings.FirstOrDefault(m =>
				m.LocalModel.Equals(localModel, StringComparison.OrdinalIgnoreCase)
				&& !string.IsNullOrWhiteSpace(m.UpstreamModel));
			if (mapping is not null)
				return mapping.UpstreamModel.Trim();
		}

		if (!string.IsNullOrWhiteSpace(_settings.Provider.DefaultModel))
			return _settings.Provider.DefaultModel.Trim();

		return localModel?.Trim() ?? string.Empty;
	}

	private string ResolveUpstreamEmbeddingsModel(string? localModel)
	{
		if (!string.IsNullOrWhiteSpace(_settings.Provider.DefaultEmbeddingModel))
			return _settings.Provider.DefaultEmbeddingModel.Trim();

		return ResolveUpstreamChatModel(localModel);
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
			["messages"] = messages.DeepClone(),
			["stream"] = source["stream"]?.GetValue<bool>() ?? false
		};

		CopyCommonGenerationOptions(source, upstream);
		return upstream;
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

	private JsonObject BuildOpenAiChatRequestFromResponses(JsonObject source)
	{
		var model = source["model"]?.GetValue<string>() ?? string.Empty;
		var request = new JsonObject
		{
			["model"] = ResolveUpstreamChatModel(model),
			["messages"] = BuildMessagesArrayFromResponsesInput(source["input"]),
			["stream"] = false
		};

		if (source["tools"] is not null)
			request["tools"] = source["tools"]!.DeepClone();
		if (source["temperature"] is not null)
			request["temperature"] = source["temperature"]!.DeepClone();
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
			foreach (var item in inputArray)
			{
				if (item is JsonObject messageObject)
				{
					if (messageObject["role"] is not null && messageObject["content"] is not null)
					{
						result.Add(new JsonObject
						{
							["role"] = messageObject["role"]!.DeepClone(),
							["content"] = ConvertResponsesContentToMessageContent(messageObject["content"])
						});
						continue;
					}

					if ((messageObject["type"]?.GetValue<string>() ?? string.Empty).Equals("message", StringComparison.OrdinalIgnoreCase))
					{
						result.Add(new JsonObject
						{
							["role"] = messageObject["role"]?.GetValue<string>() ?? "user",
							["content"] = ConvertResponsesContentToMessageContent(messageObject["content"])
						});
					}
				}
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
			return new JsonObject
			{
				["model"] = modelName,
				["created_at"] = DateTimeOffset.UtcNow.ToString("O"),
				["message"] = new JsonObject
				{
					["role"] = "assistant",
					["content"] = content
				},
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
			return new JsonObject
			{
				["model"] = modelName,
				["created_at"] = DateTimeOffset.UtcNow.ToString("O"),
				["message"] = new JsonObject
				{
					["role"] = "assistant",
					["content"] = content
				},
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

	private static string ExtractAssistantContent(JsonObject parsed) =>
		parsed["choices"]?[0]?["message"]?["content"]?.GetValue<string>()
		?? parsed["choices"]?[0]?["delta"]?["content"]?.GetValue<string>()
		?? string.Empty;

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
		if (_settings.RequestResponseLogging?.Enabled != true)
			return;

		var message = $"request {operation}";
		if (_settings.RequestResponseLogging.IncludeBodies)
			message += $": {TruncateForLog(payload.ToJsonString(JsonOptions))}";
		LogInfo(message);
	}

	private void LogResponseIfEnabled(string operation, HttpStatusCode statusCode, string body)
	{
		if (_settings.RequestResponseLogging?.Enabled != true)
			return;

		var message = $"response {operation}: {(int)statusCode} {statusCode}";
		if (_settings.RequestResponseLogging.IncludeBodies)
			message += $" | {TruncateForLog(body)}";
		LogInfo(message);
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
				await writer.WriteLineAsync(BuildOllamaStreamChunk(localModel, createdAt, isChat, string.Empty, true, "stop").ToJsonString(JsonOptions));
				break;
			}

			var eventJson = JsonNode.Parse(payload)?.AsObject();
			if (eventJson is null)
				continue;

			var chunkText = eventJson["choices"]?[0]?["delta"]?["content"]?.GetValue<string>()
				?? eventJson["choices"]?[0]?["message"]?["content"]?.GetValue<string>()
				?? string.Empty;
			var finishReason = eventJson["choices"]?[0]?["finish_reason"]?.GetValue<string>();

			if (chunkText.Length > 0)
				await writer.WriteLineAsync(BuildOllamaStreamChunk(localModel, createdAt, isChat, chunkText, false, finishReason).ToJsonString(JsonOptions));

			if (!string.IsNullOrWhiteSpace(finishReason))
			{
				await writer.WriteLineAsync(BuildOllamaStreamChunk(localModel, createdAt, isChat, string.Empty, true, finishReason).ToJsonString(JsonOptions));
				break;
			}
		}

		await output.FlushAsync(ct);
		TryCloseResponse(response);
	}

	private static JsonObject BuildOllamaStreamChunk(string model, string createdAt, bool isChat, string content, bool done, string? doneReason)
	{
		if (isChat)
		{
			return new JsonObject
			{
				["model"] = model,
				["created_at"] = createdAt,
				["message"] = new JsonObject
				{
					["role"] = "assistant",
					["content"] = content
				},
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

	private JsonObject BuildOllamaTagsResponse()
	{
		var models = new JsonArray();
		foreach (var modelName in GetLocalModelNames())
		{
			models.Add(new JsonObject
			{
				["name"] = modelName,
				["model"] = modelName,
				["modified_at"] = DateTimeOffset.UtcNow.ToString("O"),
				["size"] = 0,
				["digest"] = "trafficpilot",
				["details"] = new JsonObject
				{
					["format"] = "trafficpilot",
					["family"] = "forwarded",
					["parameter_size"] = "unknown",
					["quantization_level"] = "unknown"
				}
			});
		}

		return new JsonObject { ["models"] = models };
	}

	private JsonObject BuildOpenAiModelsResponse()
	{
		var data = new JsonArray();
		foreach (var modelName in GetLocalModelNames())
		{
			data.Add(new JsonObject
			{
				["id"] = modelName,
				["object"] = "model",
				["created"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
				["owned_by"] = _settings.Provider.Name
			});
		}

		return new JsonObject
		{
			["object"] = "list",
			["data"] = data
		};
	}

	private IReadOnlyList<string> GetLocalModelNames()
	{
		var models = _settings.ModelMappings
			.Where(static mapping => !string.IsNullOrWhiteSpace(mapping.LocalModel))
			.Select(static mapping => mapping.LocalModel.Trim())
			.ToList();

		if (!string.IsNullOrWhiteSpace(_settings.Provider.DefaultModel))
			models.Add(_settings.Provider.DefaultModel.Trim());

		if (!string.IsNullOrWhiteSpace(_settings.Provider.DefaultEmbeddingModel))
			models.Add(_settings.Provider.DefaultEmbeddingModel.Trim());

		if (models.Count == 0)
			models.Add("default");

		return models.Distinct(StringComparer.OrdinalIgnoreCase)
			.OrderBy(static model => model, StringComparer.OrdinalIgnoreCase)
			.ToList();
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

	private static void TryCloseResponse(HttpListenerResponse response)
	{
		try { response.Close(); } catch { }
	}

	private void LogInfo(string message) => OnLog?.Invoke($"[Local API] {message}");
}
