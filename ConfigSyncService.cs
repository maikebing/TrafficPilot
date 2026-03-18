using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace TrafficPilot;

// ════════════════════════════════════════════════════════════════
//  Config sync — interface & result types
// ════════════════════════════════════════════════════════════════

/// <summary>
/// Abstraction over a remote storage back-end that can save and restore
/// a single text blob identified by an opaque <see cref="RemoteId"/>.
/// </summary>
internal interface IConfigSyncProvider : IDisposable
{
	/// <summary>
	/// Uploads <paramref name="configJson"/> to the remote store.
	/// If <paramref name="existingId"/> is supplied the existing entry is updated;
	/// otherwise a new entry is created.
	/// </summary>
	/// <returns>The remote identifier of the created / updated entry.</returns>
	Task<string> PushAsync(string configJson, string? existingId = null, CancellationToken ct = default);

	/// <summary>Downloads and returns the raw JSON for the entry identified by <paramref name="remoteId"/>.</summary>
	Task<string> PullAsync(string remoteId, CancellationToken ct = default);
}

// ════════════════════════════════════════════════════════════════
//  Shared helpers
// ════════════════════════════════════════════════════════════════

internal abstract class GistProviderBase : IConfigSyncProvider
{
	protected const string FileName = "trafficpilot.json";
	protected const string GistDescription = "TrafficPilot configuration";

	protected readonly HttpClient Http;

	protected GistProviderBase(string baseAddress, string token)
	{
		Http = new HttpClient { BaseAddress = new Uri(baseAddress), Timeout = TimeSpan.FromSeconds(30) };
		Http.DefaultRequestHeaders.UserAgent.ParseAdd("TrafficPilot/1.0");
		Http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("token", token);
		Http.DefaultRequestHeaders.Accept.ParseAdd("application/json");
	}

	public async Task<string> PushAsync(string configJson, string? existingId = null, CancellationToken ct = default)
	{
		ArgumentNullException.ThrowIfNull(configJson);

		var body = BuildGistBody(configJson);
		using var content = new StringContent(body, Encoding.UTF8, "application/json");

		HttpResponseMessage response;
		if (string.IsNullOrWhiteSpace(existingId))
		{
			// Create a new gist/snippet
			response = await Http.PostAsync("gists", content, ct).ConfigureAwait(false);
		}
		else
		{
			// Update existing gist/snippet
			using var req = new HttpRequestMessage(HttpMethod.Patch, $"gists/{existingId}") { Content = content };
			response = await Http.SendAsync(req, ct).ConfigureAwait(false);
		}

		await EnsureSuccessAsync(response, ct).ConfigureAwait(false);

		await using var stream = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
		using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct).ConfigureAwait(false);

		if (!doc.RootElement.TryGetProperty("id", out var idProp) || idProp.GetString() is not { } id)
			throw new InvalidOperationException("Remote API did not return a gist/snippet ID.");

		return id;
	}

	public async Task<string> PullAsync(string remoteId, CancellationToken ct = default)
	{
		ArgumentNullException.ThrowIfNullOrWhiteSpace(remoteId);

		using var response = await Http.GetAsync($"gists/{remoteId}", ct).ConfigureAwait(false);
		await EnsureSuccessAsync(response, ct).ConfigureAwait(false);

		await using var stream = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
		using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct).ConfigureAwait(false);

		// Navigate: .files.<FileName>.content
		if (!doc.RootElement.TryGetProperty("files", out var files))
			throw new InvalidOperationException($"Remote gist/snippet has no 'files' property.");

		if (!files.TryGetProperty(FileName, out var fileNode))
			throw new InvalidOperationException($"Remote gist/snippet does not contain a '{FileName}' file.");

		// If content is null the file may be truncated; fetch raw_url instead
		if (fileNode.TryGetProperty("content", out var contentProp)
			&& contentProp.GetString() is { Length: > 0 } directContent)
		{
			return directContent;
		}

		if (fileNode.TryGetProperty("raw_url", out var rawUrlProp)
			&& rawUrlProp.GetString() is { Length: > 0 } rawUrl)
		{
			return await Http.GetStringAsync(rawUrl, ct).ConfigureAwait(false);
		}

		throw new InvalidOperationException($"'{FileName}' content is empty or missing in the remote gist/snippet.");
	}

	public void Dispose() => Http.Dispose();

	private static string BuildGistBody(string configJson)
	{
		// Serialise with System.Text.Json to ensure the JSON is valid and escaped
		using var ms = new System.IO.MemoryStream();
		using var writer = new Utf8JsonWriter(ms);

		writer.WriteStartObject();
		writer.WriteString("description", GistDescription);
		writer.WriteBoolean("public", false);
		writer.WriteStartObject("files");
		writer.WriteStartObject(FileName);
		writer.WriteString("content", configJson);
		writer.WriteEndObject();
		writer.WriteEndObject();
		writer.WriteEndObject();
		writer.Flush();

		return Encoding.UTF8.GetString(ms.ToArray());
	}

	private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken ct)
	{
		if (!response.IsSuccessStatusCode)
		{
			string body;
			try { body = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false); }
			catch { body = string.Empty; }

			Debug.WriteLine($"[ConfigSync] HTTP {(int)response.StatusCode}: {body}");
			response.EnsureSuccessStatusCode(); // throws HttpRequestException
		}
	}
}

// ════════════════════════════════════════════════════════════════
//  GitHub Gist provider
// ════════════════════════════════════════════════════════════════

/// <summary>
/// Syncs the TrafficPilot configuration to a private GitHub Gist
/// via the GitHub REST API v3 (<c>https://api.github.com</c>).
/// </summary>
internal sealed class GitHubGistProvider : GistProviderBase
{
	private const string BaseAddress = "https://api.github.com/";

	/// <param name="token">A GitHub Personal Access Token with the <c>gist</c> scope.</param>
	public GitHubGistProvider(string token) : base(BaseAddress, token)
	{
		// GitHub requires a specific Accept header for the Gists v3 API
		Http.DefaultRequestHeaders.Accept.Clear();
		Http.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
		Http.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2022-11-28");
	}
}

// ════════════════════════════════════════════════════════════════
//  Gitee Snippet (Gist) provider
// ════════════════════════════════════════════════════════════════

/// <summary>
/// Syncs the TrafficPilot configuration to a private Gitee code snippet (码云代码片段)
/// via the Gitee REST API v5 (<c>https://gitee.com/api/v5</c>).
/// </summary>
internal sealed class GiteeSnippetProvider : GistProviderBase
{
	private const string BaseAddress = "https://gitee.com/api/v5/";

	/// <param name="token">A Gitee Personal Access Token with the <c>gists</c> scope.</param>
	public GiteeSnippetProvider(string token) : base(BaseAddress, token) { }
}

// ════════════════════════════════════════════════════════════════
//  Factory
// ════════════════════════════════════════════════════════════════

/// <summary>Creates the appropriate <see cref="IConfigSyncProvider"/> for a given provider name.</summary>
internal static class ConfigSyncProviderFactory
{
	/// <summary>
	/// Creates the provider for <paramref name="provider"/> using <paramref name="token"/>.
	/// </summary>
	/// <exception cref="ArgumentException">Unknown provider name.</exception>
	public static IConfigSyncProvider Create(string provider, string token)
	{
		ArgumentNullException.ThrowIfNull(provider);
		ArgumentNullException.ThrowIfNull(token);

		return provider switch
		{
			"GitHub" => new GitHubGistProvider(token),
			"Gitee"  => new GiteeSnippetProvider(token),
			_        => throw new ArgumentException($"Unknown config sync provider: '{provider}'.", nameof(provider))
		};
	}
}
