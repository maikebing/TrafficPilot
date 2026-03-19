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
			try
			{
				listener.Close();
			}
			catch
			{
			}
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

	private IEnumerable<ushort> GetDistinctPorts()
	{
		return new[] { _settings.OllamaPort, _settings.FoundryPort }.Distinct();
	}

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
		try
		{
			var path = context.Request.Url?.AbsolutePath ?? "/";
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
				&& (path.Equals("/v1/chat/completions", StringComparison.OrdinalIgnoreCase)
					|| path.Equals("/chat/completions", StringComparison.OrdinalIgnoreCase)))
			{
				await HandleOpenAiCompatibleAsync(context, ct);
				return;
			}

			await WriteJsonAsync(context.Response, HttpStatusCode.NotFound, new JsonObject
			{
				["error"] = $"Unsupported local API path: {path}"
			});
		}
		catch (Exception ex) when (ex is not OperationCanceledException)
		{
			LogInfo($"Local API request error: {ex.Message}");
			try
			{
				await WriteJsonAsync(context.Response, HttpStatusCode.BadGateway, new JsonObject
				{
					["error"] = ex.Message
				});
			}
			catch
			{
				TryCloseResponse(context.Response);
			}
		}
	}

	private async Task HandleOpenAiCompatibleAsync(HttpListenerContext context, CancellationToken ct)
	{
		var requestJson = await ReadRequestJsonAsync(context.Request, ct);
		var requestedModel = requestJson["model"]?.GetValue<string>() ?? string.Empty;
		requestJson["model"] = ResolveUpstreamModel(requestedModel);

		using var upstreamRequest = CreateUpstreamRequest("chat/completions", requestJson);
		using var upstreamResponse = await _httpClient.SendAsync(upstreamRequest, HttpCompletionOption.ResponseHeadersRead, ct);

		LogInfo($"Forwarded OpenAI-compatible request '{requestedModel}' to {_settings.Provider.BaseUrl}");
		await CopyUpstreamResponseAsync(context.Response, upstreamResponse, passthroughStreaming: true, ct);
	}

	private async Task HandleOllamaGenerateAsync(HttpListenerContext context, CancellationToken ct)
	{
		var requestJson = await ReadRequestJsonAsync(context.Request, ct);
		var localModel = requestJson["model"]?.GetValue<string>() ?? string.Empty;
		var stream = requestJson["stream"]?.GetValue<bool>() ?? false;

		var upstreamRequestJson = BuildUpstreamChatRequestFromGenerate(requestJson);
		using var upstreamRequest = CreateUpstreamRequest("chat/completions", upstreamRequestJson);
		using var upstreamResponse = await _httpClient.SendAsync(upstreamRequest, HttpCompletionOption.ResponseHeadersRead, ct);

		LogInfo($"Forwarded Ollama generate request '{localModel}' to {_settings.Provider.BaseUrl}");
		if (stream)
		{
			await WriteOllamaStreamingResponseAsync(
				context.Response,
				upstreamResponse,
				string.IsNullOrWhiteSpace(localModel) ? ResolveUpstreamModel(string.Empty) : localModel,
				isChat: false,
				ct);
			return;
		}

		await WriteJsonAsync(
			context.Response,
			upstreamResponse.StatusCode,
			await ConvertUpstreamChatResponseToOllamaAsync(upstreamResponse, localModel, isChat: false, ct),
			contentType: "application/json");
	}

	private async Task HandleOllamaChatAsync(HttpListenerContext context, CancellationToken ct)
	{
		var requestJson = await ReadRequestJsonAsync(context.Request, ct);
		var localModel = requestJson["model"]?.GetValue<string>() ?? string.Empty;
		var stream = requestJson["stream"]?.GetValue<bool>() ?? false;

		var upstreamRequestJson = BuildUpstreamChatRequestFromChat(requestJson);
		using var upstreamRequest = CreateUpstreamRequest("chat/completions", upstreamRequestJson);
		using var upstreamResponse = await _httpClient.SendAsync(upstreamRequest, HttpCompletionOption.ResponseHeadersRead, ct);

		LogInfo($"Forwarded Ollama chat request '{localModel}' to {_settings.Provider.BaseUrl}");
		if (stream)
		{
			await WriteOllamaStreamingResponseAsync(
				context.Response,
				upstreamResponse,
				string.IsNullOrWhiteSpace(localModel) ? ResolveUpstreamModel(string.Empty) : localModel,
				isChat: true,
				ct);
			return;
		}

		await WriteJsonAsync(
			context.Response,
			upstreamResponse.StatusCode,
			await ConvertUpstreamChatResponseToOllamaAsync(upstreamResponse, localModel, isChat: true, ct),
			contentType: "application/json");
	}

	private HttpRequestMessage CreateUpstreamRequest(string relativePath, JsonNode payload)
	{
		var baseUri = NormalizeBaseUri(_settings.Provider.BaseUrl);
		var request = new HttpRequestMessage(HttpMethod.Post, new Uri(baseUri, relativePath))
		{
			Content = new StringContent(payload.ToJsonString(JsonOptions), Encoding.UTF8, "application/json")
		};

		if (!string.IsNullOrWhiteSpace(_apiKey))
			request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

		return request;
	}

	private static Uri NormalizeBaseUri(string baseUrl)
	{
		if (baseUrl.EndsWith("/", StringComparison.Ordinal))
			return new Uri(baseUrl, UriKind.Absolute);

		return new Uri($"{baseUrl}/", UriKind.Absolute);
	}

	private string ResolveUpstreamModel(string? localModel)
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
			["model"] = ResolveUpstreamModel(localModel),
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
			["model"] = ResolveUpstreamModel(localModel),
			["messages"] = messages.DeepClone(),
			["stream"] = source["stream"]?.GetValue<bool>() ?? false
		};

		CopyCommonGenerationOptions(source, upstream);
		return upstream;
	}

	private static void CopyCommonGenerationOptions(JsonObject source, JsonObject target)
	{
		foreach (var property in new[] { "temperature", "top_p", "max_tokens", "stop" })
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

	private async Task<JsonObject> ConvertUpstreamChatResponseToOllamaAsync(
		HttpResponseMessage upstreamResponse,
		string localModel,
		bool isChat,
		CancellationToken ct)
	{
		if (!upstreamResponse.IsSuccessStatusCode)
			return new JsonObject
			{
				["error"] = await ExtractUpstreamErrorAsync(upstreamResponse, ct)
			};

		var body = await upstreamResponse.Content.ReadAsStringAsync(ct);
		var parsed = JsonNode.Parse(body)?.AsObject()
			?? throw new InvalidOperationException("The upstream provider returned an empty response.");

		var content = ExtractAssistantContent(parsed);
		var finishReason = parsed["choices"]?[0]?["finish_reason"]?.GetValue<string>();
		var usage = parsed["usage"]?.AsObject();
		var modelName = string.IsNullOrWhiteSpace(localModel) ? (parsed["model"]?.GetValue<string>() ?? ResolveUpstreamModel(string.Empty)) : localModel;

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

	private static string ExtractAssistantContent(JsonObject parsed)
	{
		return parsed["choices"]?[0]?["message"]?["content"]?.GetValue<string>()
			?? parsed["choices"]?[0]?["delta"]?["content"]?.GetValue<string>()
			?? string.Empty;
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
			await WriteJsonAsync(response, upstreamResponse.StatusCode, new JsonObject
			{
				["error"] = await ExtractUpstreamErrorAsync(upstreamResponse, ct)
			});
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
		{
			TryAddResponseHeader(response, header.Key, header.Value);
		}

		foreach (var header in upstreamResponse.Content.Headers)
		{
			TryAddResponseHeader(response, header.Key, header.Value);
		}

		var output = response.OutputStream;
		await using var input = await upstreamResponse.Content.ReadAsStreamAsync(ct);
		await input.CopyToAsync(output, ct);
		await output.FlushAsync(ct);
		TryCloseResponse(response);
	}

	private static bool IsEventStream(HttpResponseMessage response)
	{
		return response.Content.Headers.ContentType?.MediaType?.Equals("text/event-stream", StringComparison.OrdinalIgnoreCase) == true;
	}

	private static void TryAddResponseHeader(HttpListenerResponse response, string key, IEnumerable<string> values)
	{
		if (key.Equals("Transfer-Encoding", StringComparison.OrdinalIgnoreCase)
			|| key.Equals("Content-Length", StringComparison.OrdinalIgnoreCase)
			|| key.Equals("Content-Type", StringComparison.OrdinalIgnoreCase))
			return;

		try
		{
			response.Headers[key] = string.Join(",", values);
		}
		catch
		{
		}
	}

	private async Task<string> ExtractUpstreamErrorAsync(HttpResponseMessage response, CancellationToken ct)
	{
		var body = await response.Content.ReadAsStringAsync(ct);
		if (string.IsNullOrWhiteSpace(body))
			return $"Upstream provider returned {(int)response.StatusCode} {response.ReasonPhrase}.";

		try
		{
			var parsed = JsonNode.Parse(body);
			var message = parsed?["error"]?["message"]?.GetValue<string>()
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

		return new JsonObject
		{
			["models"] = models
		};
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

		if (models.Count == 0)
			models.Add("default");

		return models
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.OrderBy(static model => model, StringComparer.OrdinalIgnoreCase)
			.ToList();
	}

	private static bool HttpMethodsEqual(string? actual, string expected)
	{
		return string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase);
	}

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
		try
		{
			response.Close();
		}
		catch
		{
		}
	}

	private void LogInfo(string message)
	{
		OnLog?.Invoke($"[Local API] {message}");
	}
}
