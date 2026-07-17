using AsepriteInstaller.State;
using AsepriteInstaller.Tui;
using AsepriteInstaller.Utils;

namespace AsepriteInstaller.Installer.Steps;

/// <summary>
/// Step 8: Post-install cleanup.
/// Optionally removes the build directory to save space.
/// Tools, deps, and source are always kept for faster future updates.
/// </summary>
public sealed class CleanupStep : IInstallerStep
{
    public string StepId => "cleanup";
    public string DisplayName => "Post-install cleanup";

    public Task<bool> ExecuteAsync(InstallContext ctx, CancellationToken ct = default)
    {
        // --- Clean build directory if user opted not to keep artifacts ---
        if (!ctx.Options.KeepBuildArtifacts)
        {
            if (Directory.Exists(ctx.BuildDir))
            {
                ctx.Log.Info("Cleaning build directory...");
                PathUtils.DeleteDirectorySafe(ctx.BuildDir);
                ctx.Log.Info("Build directory cleaned.");
            }
        }
        else
        {
            ctx.Log.Info("Keeping build artifacts for faster future updates.");
        }

        // --- Clean staging ---
        if (Directory.Exists(ctx.StagingDir))
        {
            PathUtils.DeleteDirectorySafe(ctx.StagingDir);
        }

        // --- Report disk usage ---
        var toolsSize = PathUtils.GetDirectorySize(ctx.ToolsDir);
        var depsSize = PathUtils.GetDirectorySize(ctx.DepsDir);
        var srcSize = PathUtils.GetDirectorySize(ctx.SrcDir);
        var buildSize = Directory.Exists(ctx.BuildDir) ? PathUtils.GetDirectorySize(ctx.BuildDir) : 0;

        ctx.Log.Info($"Disk usage — Tools: {toolsSize / 1024.0 / 1024:F0} MB, " +
                     $"Deps: {depsSize / 1024.0 / 1024:F0} MB, " +
                     $"Source: {srcSize / 1024.0 / 1024:F0} MB, " +
                     $"Build: {buildSize / 1024.0 / 1024:F0} MB");

        return Task.FromResult(true);
    }
}
