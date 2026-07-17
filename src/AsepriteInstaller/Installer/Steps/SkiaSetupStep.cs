using AsepriteInstaller.State;
using AsepriteInstaller.Utils;

namespace AsepriteInstaller.Installer.Steps;

/// <summary>
/// Step 4: Download and extract the Skia prebuilt library.
/// The Skia version tag is read from the Aseprite source's laf/misc/skia-tag.txt
/// (if the source is already cloned), or falls back to a known-good default.
/// The prebuilt ZIP is downloaded from GitHub releases and extracted to deps/skia/.
/// Idempotent: if skia.lib already exists, it is skipped.
/// </summary>
public sealed class SkiaSetupStep : IInstallerStep
{
    public string StepId => "skia-setup";
    public string DisplayName => "Download Skia prebuilt library";

    // Fallback tag if the source hasn't been cloned yet.
    private const string DefaultSkiaTag = "m124-08a5439a6b";

    /// <summary>
    /// Never skip — ExecuteAsync is already idempotent (checks skia.lib existence)
    /// and must always run to populate ctx.SkiaDir/SkiaLibDir paths.
    /// </summary>
    public bool CanSkip(InstallContext ctx) => false;

    public async Task<bool> ExecuteAsync(InstallContext ctx, CancellationToken ct = default)
    {
        // --- Determine Skia tag ---
        var tag = ResolveSkiaTag(ctx);
        ctx.SkiaTag = tag;
        ctx.Log.Info($"Skia tag: {tag}");

        // --- Check if already downloaded ---
        var skiaLibDir = Path.Combine(ctx.SkiaDir, "out", "Release-x64");
        var skiaLib = Path.Combine(skiaLibDir, "skia.lib");
        if (File.Exists(skiaLib))
        {
            ctx.Log.Info($"Skia already present at {ctx.SkiaDir} — skipping.");
            ctx.SkiaLibDir = skiaLibDir;
            return true;
        }

        // --- Download ---
        var url = $"https://github.com/aseprite/skia/releases/download/{tag}/Skia-Windows-Release-x64.zip";
        ctx.Log.Info($"Downloading Skia from {url}");

        using var downloader = new Downloader(ctx.Log);
        var zipPath = Path.Combine(ctx.DepsDir, "skia.zip");
        await downloader.DownloadAsync(url, zipPath, $"Skia {tag}", ct);

        // --- Extract ---
        // Extract to a temp dir first, then move to final location (atomic-ish).
        var extractTemp = Path.Combine(ctx.DepsDir, "skia-tmp");
        PathUtils.DeleteDirectorySafe(extractTemp);
        ArchiveExtractor.ExtractZipWithProgress(zipPath, extractTemp, ctx.Log);

        // The ZIP may extract to a top-level "skia" folder or directly.
        // Normalize: we want the contents at ctx.SkiaDir.
        PathUtils.DeleteDirectorySafe(ctx.SkiaDir);
        var nestedSkia = Path.Combine(extractTemp, "skia");
        if (Directory.Exists(nestedSkia))
        {
            PathUtils.MoveDirectory(nestedSkia, ctx.SkiaDir);
            PathUtils.DeleteDirectorySafe(extractTemp);
        }
        else
        {
            PathUtils.MoveDirectory(extractTemp, ctx.SkiaDir);
        }

        File.Delete(zipPath);

        // --- Verify ---
        if (!File.Exists(skiaLib))
        {
            ctx.Log.Error($"Skia library not found at expected path: {skiaLib}");
            ctx.Log.Info("Contents of skia directory:");
            if (Directory.Exists(ctx.SkiaDir))
            {
                foreach (var entry in Directory.GetFileSystemEntries(ctx.SkiaDir))
                    ctx.Log.Info($"  {entry}");
            }
            return false;
        }

        ctx.SkiaLibDir = skiaLibDir;
        ctx.Log.Success($"Skia installed at {ctx.SkiaDir}");
        return true;
    }

    /// <summary>
    /// Read the Skia tag from the Aseprite source if available,
    /// otherwise use the default.
    /// </summary>
    private static string ResolveSkiaTag(InstallContext ctx)
    {
        var tagFile = Path.Combine(ctx.AsepriteSrcDir, "laf", "misc", "skia-tag.txt");
        if (File.Exists(tagFile))
        {
            var tag = File.ReadAllText(tagFile).Trim();
            if (!string.IsNullOrEmpty(tag))
            {
                ctx.Log.Info($"Read Skia tag from source: {tag}");
                return tag;
            }
        }

        ctx.Log.Info($"Using default Skia tag: {DefaultSkiaTag}");
        return DefaultSkiaTag;
    }

    public Task CleanupAsync(InstallContext ctx, CancellationToken ct = default)
    {
        // Remove partial download.
        var zipPath = Path.Combine(ctx.DepsDir, "skia.zip");
        if (File.Exists(zipPath)) File.Delete(zipPath);
        var extractTemp = Path.Combine(ctx.DepsDir, "skia-tmp");
        PathUtils.DeleteDirectorySafe(extractTemp);
        return Task.CompletedTask;
    }
}
