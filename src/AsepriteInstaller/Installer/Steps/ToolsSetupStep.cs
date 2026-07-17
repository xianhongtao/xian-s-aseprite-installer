using AsepriteInstaller.State;
using AsepriteInstaller.Utils;

namespace AsepriteInstaller.Installer.Steps;

/// <summary>
/// Step 3: Download and set up portable build tools (CMake, Ninja, MinGit).
/// Each tool is downloaded as a ZIP, extracted to the tools directory, and
/// its executable path is stored in the context.
/// Idempotent: if the tool binary already exists, it is skipped.
/// </summary>
public sealed class ToolsSetupStep : IInstallerStep
{
    public string StepId => "tools-setup";
    public string DisplayName => "Download build tools (CMake, Ninja, Git)";

    // Pinned versions for reproducibility.
    private const string CMakeVersion = "3.31.12";
    private const string CMakeUrl =
        $"https://github.com/Kitware/CMake/releases/download/v{CMakeVersion}/cmake-{CMakeVersion}-windows-x86_64.zip";

    private const string NinjaVersion = "1.13.2";
    private const string NinjaUrl =
        $"https://github.com/ninja-build/ninja/releases/download/v{NinjaVersion}/ninja-win.zip";

    private const string GitVersion = "2.55.0.3";
    private const string GitUrl =
        $"https://github.com/git-for-windows/git/releases/download/v2.55.0.windows.3/MinGit-{GitVersion}-64-bit.zip";

    /// <summary>
    /// Never skip — ExecuteAsync is already idempotent (checks file existence)
    /// and must always run to populate ctx.CMakeExe/NinjaExe/GitExe paths.
    /// </summary>
    public bool CanSkip(InstallContext ctx) => false;

    public async Task<bool> ExecuteAsync(InstallContext ctx, CancellationToken ct = default)
    {
        using var downloader = new Downloader(ctx.Log);

        // --- CMake ---
        var cmakeDir = Path.Combine(ctx.ToolsDir, "cmake");
        var cmakeExe = Path.Combine(cmakeDir, "bin", "cmake.exe");
        if (File.Exists(cmakeExe))
        {
            ctx.Log.Info($"CMake already present at {cmakeDir} — skipping.");
        }
        else
        {
            var zipPath = Path.Combine(ctx.ToolsDir, "cmake.zip");
            await downloader.DownloadAsync(CMakeUrl, zipPath, $"CMake {CMakeVersion}", ct);
            ArchiveExtractor.ExtractZipWithProgress(zipPath, cmakeDir, ctx.Log);

            // The ZIP extracts to a top-level folder like "cmake-3.31.12-windows-x86_64".
            // Move its contents up one level.
            var nestedDir = Path.Combine(cmakeDir, $"cmake-{CMakeVersion}-windows-x86_64");
            if (Directory.Exists(nestedDir))
            {
                foreach (var entry in Directory.GetFileSystemEntries(nestedDir))
                {
                    var dest = Path.Combine(cmakeDir, Path.GetFileName(entry));
                    if (Directory.Exists(entry))
                        PathUtils.MoveDirectory(entry, dest);
                    else
                        File.Move(entry, dest, overwrite: true);
                }
                PathUtils.DeleteDirectorySafe(nestedDir);
            }

            File.Delete(zipPath);

            if (!File.Exists(cmakeExe))
            {
                ctx.Log.Error($"CMake binary not found at expected path: {cmakeExe}");
                return false;
            }
        }
        ctx.CMakeExe = cmakeExe;
        ctx.Log.Info($"CMake: {cmakeExe} ✓");

        // --- Ninja ---
        var ninjaDir = Path.Combine(ctx.ToolsDir, "ninja");
        var ninjaExe = Path.Combine(ninjaDir, "ninja.exe");
        if (File.Exists(ninjaExe))
        {
            ctx.Log.Info($"Ninja already present at {ninjaDir} — skipping.");
        }
        else
        {
            var zipPath = Path.Combine(ctx.ToolsDir, "ninja.zip");
            await downloader.DownloadAsync(NinjaUrl, zipPath, $"Ninja {NinjaVersion}", ct);
            ArchiveExtractor.ExtractZipWithProgress(zipPath, ninjaDir, ctx.Log);
            File.Delete(zipPath);

            if (!File.Exists(ninjaExe))
            {
                ctx.Log.Error($"Ninja binary not found at expected path: {ninjaExe}");
                return false;
            }
        }
        ctx.NinjaExe = ninjaExe;
        ctx.Log.Info($"Ninja: {ninjaExe} ✓");

        // --- Git (MinGit) ---
        var gitDir = Path.Combine(ctx.ToolsDir, "git");
        var gitExe = Path.Combine(gitDir, "cmd", "git.exe");
        if (File.Exists(gitExe))
        {
            ctx.Log.Info($"Git already present at {gitDir} — skipping.");
        }
        else
        {
            var zipPath = Path.Combine(ctx.ToolsDir, "mingit.zip");
            await downloader.DownloadAsync(GitUrl, zipPath, $"MinGit {GitVersion}", ct);
            ArchiveExtractor.ExtractZipWithProgress(zipPath, gitDir, ctx.Log);
            File.Delete(zipPath);

            if (!File.Exists(gitExe))
            {
                ctx.Log.Error($"Git binary not found at expected path: {gitExe}");
                return false;
            }
        }
        ctx.GitExe = gitExe;
        ctx.Log.Info($"Git: {gitExe} ✓");

        return true;
    }
}
