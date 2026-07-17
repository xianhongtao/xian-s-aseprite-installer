using System.Diagnostics;
using System.Net.Http;
using Spectre.Console;

namespace AsepriteInstaller.Utils;

/// <summary>
/// HTTP downloader with progress reporting, retry, and SHA-256 verification.
/// Uses HttpClientHandler with automatic decompression — AOT-compatible.
/// </summary>
public sealed class Downloader : IDisposable
{
    private readonly HttpClient _client;
    private readonly Logger _log;
    private const int MaxRetries = 3;

    public Downloader(Logger log)
    {
        _log = log;
        var handler = new HttpClientHandler();
        _client = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromMinutes(30),
        };
        _client.DefaultRequestHeaders.Add("User-Agent", "xian's-Aseprite-Installer/1.0");
    }

    /// <summary>
    /// Download a file with a Spectre progress bar.
    /// Returns the path to the downloaded file.
    /// </summary>
    public async Task<string> DownloadAsync(
        string url,
        string destPath,
        string? description = null,
        CancellationToken ct = default)
    {
        description ??= Path.GetFileName(destPath);

        for (int attempt = 1; attempt <= MaxRetries; attempt++)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(destPath)!);

                // Get content length for progress bar.
                using var headResp = await _client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);
                headResp.EnsureSuccessStatusCode();
                var totalBytes = headResp.Content.Headers.ContentLength ?? -1;

                using var resp = await _client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);
                resp.EnsureSuccessStatusCode();

                await AnsiConsole.Progress()
                    .Columns(new ProgressColumn[]
                    {
                        new TaskDescriptionColumn(),
                        new ProgressBarColumn(),
                        new PercentageColumn(),
                        new DownloadedColumn(),
                        new TransferSpeedColumn(),
                        new RemainingTimeColumn(),
                        new SpinnerColumn(),
                    })
                    .StartAsync(async ctx =>
                    {
                        var task = ctx.AddTask($"[cyan]{description}[/]", new ProgressTaskSettings
                        {
                            MaxValue = totalBytes > 0 ? totalBytes : double.MaxValue,
                        });

                        using var stream = await resp.Content.ReadAsStreamAsync(ct);
                        using var fs = new FileStream(destPath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, useAsync: true);
                        var buffer = new byte[81920];
                        int read;
                        while ((read = await stream.ReadAsync(buffer, ct)) > 0)
                        {
                            await fs.WriteAsync(buffer.AsMemory(0, read), ct);
                            task.Increment(read);
                        }
                        task.Value = task.MaxValue;
                    });

                _log.Info($"Downloaded {description} ({new FileInfo(destPath).Length / 1024.0 / 1024.0:F1} MB)");
                return destPath;
            }
            catch (Exception ex) when (attempt < MaxRetries)
            {
                _log.Warn($"Download attempt {attempt}/{MaxRetries} failed: {ex.Message}. Retrying...");
                await Task.Delay(2000 * attempt, ct);
            }
        }

        // Final attempt without catch.
        using var finalResp = await _client.GetAsync(url, ct);
        finalResp.EnsureSuccessStatusCode();
        using (var fs = File.Create(destPath))
            await finalResp.Content.CopyToAsync(fs, ct);
        return destPath;
    }

    /// <summary>Download a small text file and return its content.</summary>
    public async Task<string> DownloadTextAsync(string url, CancellationToken ct = default)
    {
        for (int attempt = 1; attempt <= MaxRetries; attempt++)
        {
            try
            {
                return await _client.GetStringAsync(url, ct);
            }
            catch (Exception ex) when (attempt < MaxRetries)
            {
                _log.Warn($"Download text attempt {attempt}/{MaxRetries} failed: {ex.Message}. Retrying...");
                await Task.Delay(2000 * attempt, ct);
            }
        }
        return await _client.GetStringAsync(url, ct);
    }

    /// <summary>Compute SHA-256 of a file.</summary>
    public static string ComputeSha256(string filePath)
    {
        using var sha = System.Security.Cryptography.SHA256.Create();
        using var stream = File.OpenRead(filePath);
        var hash = sha.ComputeHash(stream);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    public void Dispose() => _client.Dispose();
}
