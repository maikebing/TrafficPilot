using System.Diagnostics;
using System.IO.Compression;
using System.Reflection;
using System.Text.Json;

namespace TrafficPilot;

// ════════════════════════════════════════════════════════════════
//  Auto-updater: checks GitHub & Gitee releases, downloads & applies update
// ════════════════════════════════════════════════════════════════

/// <summary>Describes an available release retrieved from a remote source.</summary>
internal sealed record ReleaseInfo(Version Version, string DownloadUrl, string Source);

/// <summary>
/// Checks GitHub and Gitee releases APIs for newer versions, downloads the zip artifact,
/// and launches a PowerShell script to replace the running executable after the process exits.
/// </summary>
internal sealed class AutoUpdater : IDisposable
{
    private const string GitHubApiUrl = "https://api.github.com/repos/maikebing/TrafficPilot/releases/latest";
    private const string GiteeApiUrl = "https://gitee.com/api/v5/repos/maikebing/TrafficPilot/releases/latest";

    private readonly HttpClient _http;

    public AutoUpdater()
    {
        _http = new HttpClient();
        _http.DefaultRequestHeaders.UserAgent.ParseAdd("TrafficPilot-AutoUpdater/1.0");
        _http.Timeout = TimeSpan.FromSeconds(30);
    }

    /// <summary>
    /// Queries GitHub and Gitee for the latest release and returns the best available result.
    /// </summary>
    public async Task<ReleaseInfo?> GetLatestReleaseAsync(CancellationToken ct = default)
    {
        ReleaseInfo? best = null;
        List<Exception>? errors = null;

        foreach (var (url, source) in new[] { (GitHubApiUrl, "GitHub"), (GiteeApiUrl, "Gitee") })
        {
            try
            {
                var info = await FetchReleaseInfoAsync(url, source, ct).ConfigureAwait(false);
                if (info is not null && (best is null || info.Version > best.Version))
                    best = info;
            }
            catch (HttpRequestException ex)
            {
                Debug.WriteLine($"[AutoUpdater] {source} check failed: {ex.Message}");
                (errors ??= []).Add(ex);
            }
            catch (TaskCanceledException ex)
            {
                Debug.WriteLine($"[AutoUpdater] {source} check timed out: {ex.Message}");
                (errors ??= []).Add(ex);
            }
            catch (JsonException ex)
            {
                Debug.WriteLine($"[AutoUpdater] {source} response parse failed: {ex.Message}");
                (errors ??= []).Add(ex);
            }
        }

        if (best is not null)
            return best;

        if (errors is { Count: > 0 })
            throw new HttpRequestException("Failed to query the online release sources.", errors.Count == 1 ? errors[0] : new AggregateException(errors));

        return null;
    }

    /// <summary>
    /// Queries GitHub and Gitee for the latest release. Returns the best available
    /// <see cref="ReleaseInfo"/> if it is newer than <paramref name="currentVersion"/>,
    /// or <c>null</c> if already up to date.
    /// </summary>
    public async Task<ReleaseInfo?> CheckForUpdateAsync(Version currentVersion, CancellationToken ct = default)
    {
        var best = await GetLatestReleaseAsync(ct).ConfigureAwait(false);
        var current = NormalizeVersion(currentVersion);

        return best is not null && best.Version > current ? best : null;
    }

    /// <summary>
    /// Downloads the release zip, extracts it, and launches a PowerShell updater script
    /// that waits for this process to exit before copying the new files and restarting the app.
    /// The caller should call <c>Application.Exit()</c> immediately after this returns.
    /// </summary>
    public async Task DownloadAndApplyUpdateAsync(
        ReleaseInfo release,
        IProgress<(int Percent, string Message)>? progress = null,
        CancellationToken ct = default)
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"TrafficPilot-update-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            var zipPath = Path.Combine(tempDir, "update.zip");

            progress?.Report((0, "Downloading update..."));
            await DownloadWithProgressAsync(release.DownloadUrl, zipPath, progress, ct).ConfigureAwait(false);

            progress?.Report((90, "Extracting archive..."));
            var extractDir = Path.Combine(tempDir, "extracted");
            ZipFile.ExtractToDirectory(zipPath, extractDir, overwriteFiles: true);

            var contentDir = FindContentDirectory(extractDir);
            var exePath = Environment.ProcessPath
                ?? System.AppContext.BaseDirectory;
            var appDir = Path.GetDirectoryName(exePath)!;

            progress?.Report((95, "Preparing updater..."));
            var scriptPath = Path.Combine(tempDir, "apply-update.ps1");
            WriteUpdaterScript(scriptPath, contentDir, appDir, exePath, tempDir);

            progress?.Report((100, "Ready to restart."));

            Process.Start(new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-ExecutionPolicy Bypass -WindowStyle Hidden -File \"{scriptPath}\"",
                UseShellExecute = true
            });
        }
        catch
        {
            try { Directory.Delete(tempDir, recursive: true); } catch { }
            throw;
        }
    }

    public void Dispose() => _http.Dispose();

    private async Task<ReleaseInfo?> FetchReleaseInfoAsync(string apiUrl, string source, CancellationToken ct)
    {
        using var response = await _http.GetAsync(apiUrl, ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct).ConfigureAwait(false);
        var root = doc.RootElement;

        if (!root.TryGetProperty("tag_name", out var tagProp)) return null;
        var versionStr = (tagProp.GetString() ?? "").TrimStart('v');
        if (!Version.TryParse(versionStr, out var version)) return null;

        string? downloadUrl = null;
        if (root.TryGetProperty("assets", out var assets))
        {
            foreach (var asset in assets.EnumerateArray())
            {
                if (asset.TryGetProperty("name", out var nameProp)
                    && asset.TryGetProperty("browser_download_url", out var urlProp))
                {
                    var name = nameProp.GetString();
                    var url = urlProp.GetString();
                    if (name?.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) == true && url is not null)
                    {
                        downloadUrl = url;
                        break;
                    }
                }
            }
        }

        return downloadUrl is null ? null : new ReleaseInfo(NormalizeVersion(version), downloadUrl, source);
    }

    private async Task DownloadWithProgressAsync(
        string url,
        string destPath,
        IProgress<(int Percent, string Message)>? progress,
        CancellationToken ct)
    {
        using var response = await _http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var totalBytes = response.Content.Headers.ContentLength ?? -1L;
        await using var responseStream = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
        await using var fileStream = File.Create(destPath);

        var buffer = new byte[81920];
        long downloaded = 0;
        int bytesRead;

        while ((bytesRead = await responseStream.ReadAsync(buffer, ct).ConfigureAwait(false)) > 0)
        {
            await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), ct).ConfigureAwait(false);
            downloaded += bytesRead;

            if (totalBytes > 0)
            {
                int percent = (int)(downloaded * 85L / totalBytes);
                progress?.Report((percent, $"Downloading... {downloaded / 1024:N0} KB / {totalBytes / 1024:N0} KB"));
            }
        }
    }

    // If the zip contains a single top-level subdirectory (as the CI produces), return that;
    // otherwise return the extraction root itself.
    private static string FindContentDirectory(string extractDir)
    {
        var subdirs = Directory.GetDirectories(extractDir);
        return subdirs.Length == 1 ? subdirs[0] : extractDir;
    }

    // Writes a PowerShell script that waits for the current process to exit,
    // copies the new files into the app directory, and restarts the executable.
    private static void WriteUpdaterScript(
        string scriptPath,
        string sourceDir,
        string destDir,
        string exePath,
        string tempDir)
    {
        static string Esc(string s) => s.Replace("'", "''");

        var pid = Environment.ProcessId;
        using var w = new StreamWriter(scriptPath, append: false, System.Text.Encoding.UTF8);
        w.WriteLine($"$targetPid = {pid}");
        w.WriteLine($"$sourceDir = '{Esc(sourceDir)}'");
        w.WriteLine($"$destDir   = '{Esc(destDir)}'");
        w.WriteLine($"$exePath   = '{Esc(exePath)}'");
        w.WriteLine($"$tempDir   = '{Esc(tempDir)}'");
        w.WriteLine();
        w.WriteLine("$waited = 0");
        w.WriteLine("while ((Get-Process -Id $targetPid -ErrorAction SilentlyContinue) -and ($waited -lt 60)) {");
        w.WriteLine("    Start-Sleep -Milliseconds 500");
        w.WriteLine("    $waited += 0.5");
        w.WriteLine("}");
        w.WriteLine();
        w.WriteLine("Copy-Item -Path \"$sourceDir\\*\" -Destination $destDir -Recurse -Force");
        w.WriteLine();
        w.WriteLine("Start-Process -FilePath $exePath");
        w.WriteLine();
        w.WriteLine("Start-Sleep -Seconds 3");
        w.WriteLine("Remove-Item -Path $tempDir -Recurse -Force -ErrorAction SilentlyContinue");
    }

    // Normalise to Major.Minor.Build so version tags like "v1.2.3" compare
    // correctly against assembly versions like "1.2.3.0".
    private static Version NormalizeVersion(Version v) =>
        new(v.Major, Math.Max(0, v.Minor), Math.Max(0, v.Build));
}
