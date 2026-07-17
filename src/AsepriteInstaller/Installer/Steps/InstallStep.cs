using System.Diagnostics;
using AsepriteInstaller.Localization;
using AsepriteInstaller.State;
using AsepriteInstaller.Utils;
using Spectre.Console;

namespace AsepriteInstaller.Installer.Steps;

/// <summary>
/// Step 7: Atomically install the built Aseprite to the final destination.
/// Copies build artifacts to a staging directory, verifies, then atomically
/// swaps into the install location. On failure, restores the previous version.
/// Also creates a Start Menu shortcut.
/// </summary>
public sealed class InstallStep : IInstallerStep
{
    public string StepId => "install";
    public string DisplayName => Translations.StepInstall;

    public async Task<bool> ExecuteAsync(InstallContext ctx, CancellationToken ct = default)
    {
        var buildBinDir = Path.Combine(ctx.BuildDir, "bin");
        var builtExe = Path.Combine(buildBinDir, "aseprite.exe");

        if (!File.Exists(builtExe))
        {
            ctx.Log.Error($"Built aseprite.exe not found at {builtExe}");
            return false;
        }

        // --- Stage artifacts ---
        var stagingAppDir = Path.Combine(ctx.StagingDir, "Aseprite");
        PathUtils.DeleteDirectorySafe(stagingAppDir);
        Directory.CreateDirectory(stagingAppDir);

        ctx.Log.Info("Staging build artifacts...");

        // Copy aseprite.exe
        File.Copy(builtExe, Path.Combine(stagingAppDir, "aseprite.exe"), overwrite: true);

        // Copy data/ directory
        var dataDir = Path.Combine(buildBinDir, "data");
        if (Directory.Exists(dataDir))
        {
            PathUtils.CopyDirectory(dataDir, Path.Combine(stagingAppDir, "data"));
        }
        else
        {
            // Fallback: copy from source data/
            var srcDataDir = Path.Combine(ctx.AsepriteSrcDir, "data");
            if (Directory.Exists(srcDataDir))
                PathUtils.CopyDirectory(srcDataDir, Path.Combine(stagingAppDir, "data"));
        }

        // Copy license / doc files
        foreach (var file in new[] { "README.md", "EULA.txt", "LICENSES.md", "AUTHORS.md" })
        {
            var src = Path.Combine(ctx.AsepriteSrcDir, file);
            if (File.Exists(src))
                File.Copy(src, Path.Combine(stagingAppDir, file), overwrite: true);
        }

        // --- Verify staging ---
        var stagedExe = Path.Combine(stagingAppDir, "aseprite.exe");
        if (!File.Exists(stagedExe))
        {
            ctx.Log.Error("Staging verification failed: aseprite.exe missing.");
            return false;
        }
        ctx.Log.Info("Staging verified ✓");

        // --- Atomic swap ---
        var installDir = ctx.Options.InstallDir;
        var backupDir = installDir + ".bak";

        // Ensure parent directory exists.
        var parentDir = Path.GetDirectoryName(installDir);
        if (!string.IsNullOrEmpty(parentDir))
            Directory.CreateDirectory(parentDir);

        // If target exists, back it up.
        var hadExisting = Directory.Exists(installDir);
        if (hadExisting)
        {
            ctx.Log.Info($"Backing up existing installation to {backupDir}");
            PathUtils.DeleteDirectorySafe(backupDir);
            try
            {
                Directory.Move(installDir, backupDir);
            }
            catch
            {
                // If rename fails, try copy+delete.
                PathUtils.CopyDirectory(installDir, backupDir);
                PathUtils.DeleteDirectorySafe(installDir);
            }
        }

        // Move staging to install location.
        try
        {
            ctx.Log.Info($"Installing to {installDir}");
            PathUtils.MoveDirectory(stagingAppDir, installDir);
        }
        catch (Exception ex)
        {
            ctx.Log.Error($"Failed to move staged files to install location: {ex.Message}");

            // Rollback: restore backup.
            if (hadExisting && Directory.Exists(backupDir))
            {
                ctx.Log.Info("Rolling back to previous installation...");
                try
                {
                    PathUtils.MoveDirectory(backupDir, installDir);
                    ctx.Log.Info("Previous installation restored.");
                }
                catch (Exception rollbackEx)
                {
                    ctx.Log.Error($"Rollback failed: {rollbackEx.Message}");
                }
            }
            return false;
        }

        // Success — remove backup.
        if (hadExisting)
        {
            PathUtils.DeleteDirectorySafe(backupDir);
        }

        ctx.Log.Success($"Aseprite installed to {installDir}");

        // --- Create Start Menu shortcut ---
        if (ctx.Options.CreateShortcut)
        {
            CreateShortcut(ctx, installDir);
        }

        return true;
    }

    /// <summary>Create a Start Menu shortcut using PowerShell (AOT-compatible, no COM).</summary>
    private void CreateShortcut(InstallContext ctx, string installDir)
    {
        try
        {
            var exePath = Path.Combine(installDir, "aseprite.exe");
            // Use "xian's Aseprite" as the folder name to distinguish from official version.
            var shortcutDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Programs),
                "xian's Aseprite");
            Directory.CreateDirectory(shortcutDir);
            // Name the shortcut "Aseprite (self-compiled)" to distinguish from official.
            var shortcutPath = Path.Combine(shortcutDir, "Aseprite (self-compiled).lnk");

            // Use PowerShell to create the shortcut (avoids COM/WshShell in AOT).
            var psScript =
                $"$ws = New-Object -ComObject WScript.Shell; " +
                $"$s = $ws.CreateShortcut('{shortcutPath}'); " +
                $"$s.TargetPath = '{exePath}'; " +
                $"$s.WorkingDirectory = '{installDir}'; " +
                $"$s.Description = 'Aseprite (self-compiled via xian''s Aseprite Installer)'; " +
                $"$s.Save()";

            var psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -Command \"{psScript}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            };

            using var p = Process.Start(psi);
            if (p != null)
            {
                p.WaitForExit(10000);
                if (p.ExitCode == 0)
                    ctx.Log.Info($"Start Menu shortcut created: {shortcutPath}");
                else
                    ctx.Log.Warn($"Shortcut creation returned exit code {p.ExitCode}");
            }
        }
        catch (Exception ex)
        {
            ctx.Log.Warn($"Failed to create shortcut: {ex.Message}");
        }
    }

    public Task CleanupAsync(InstallContext ctx, CancellationToken ct = default)
    {
        // Clean up staging on failure.
        var stagingAppDir = Path.Combine(ctx.StagingDir, "Aseprite");
        if (Directory.Exists(stagingAppDir))
            PathUtils.DeleteDirectorySafe(stagingAppDir);
        return Task.CompletedTask;
    }
}
