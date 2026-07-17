using AsepriteInstaller.State;
using AsepriteInstaller.Utils;

namespace AsepriteInstaller.Installer.Steps;

/// <summary>
/// Step 5: Clone or update the Aseprite source repository with all submodules.
/// Uses the portable Git from ToolsSetupStep.
/// Idempotent: if the repo already exists, pulls and updates submodules instead.
/// </summary>
public sealed class SourceCloneStep : IInstallerStep
{
    public string StepId => "source-clone";
    public string DisplayName => "Clone Aseprite source code";

    private const string RepoUrl = "https://github.com/aseprite/aseprite.git";

    public async Task<bool> ExecuteAsync(InstallContext ctx, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(ctx.GitExe))
        {
            ctx.Log.Error("Git is not available. Run ToolsSetupStep first.");
            return false;
        }

        var runner = new ProcessRunner(ctx.Log);

        if (Directory.Exists(ctx.AsepriteSrcDir) && Directory.Exists(Path.Combine(ctx.AsepriteSrcDir, ".git")))
        {
            // --- Update existing clone ---
            ctx.Log.Info("Aseprite source already cloned. Updating...");

            var pullResult = await runner.RunAsync(
                ctx.GitExe, "pull --ff-only",
                workingDir: ctx.AsepriteSrcDir, ct: ct);

            if (!pullResult.Success)
            {
                ctx.Log.Warn("git pull failed — trying fetch + reset instead.");
                await runner.RunAsync(ctx.GitExe, "fetch origin", ctx.AsepriteSrcDir, ct: ct);
                await runner.RunAsync(ctx.GitExe, "reset --hard origin/main", ctx.AsepriteSrcDir, ct: ct);
            }
        }
        else
        {
            // --- Fresh clone ---
            ctx.Log.Info($"Cloning Aseprite from {RepoUrl}...");

            // Remove any partial directory.
            PathUtils.DeleteDirectorySafe(ctx.AsepriteSrcDir);

            var cloneArgs = $"clone --recursive {RepoUrl} \"{ctx.AsepriteSrcDir}\"";
            var cloneResult = await runner.RunAsync(
                ctx.GitExe, cloneArgs,
                workingDir: ctx.SrcDir, showOutput: true, ct: ct);

            if (!cloneResult.Success)
            {
                ctx.Log.Error($"git clone failed with exit code {cloneResult.ExitCode}");
                return false;
            }
        }

        // --- Checkout specific ref if requested ---
        if (!string.IsNullOrEmpty(ctx.Options.GitRef))
        {
            ctx.Log.Info($"Checking out ref: {ctx.Options.GitRef}");
            var checkoutResult = await runner.RunAsync(
                ctx.GitExe, $"checkout {ctx.Options.GitRef}",
                workingDir: ctx.AsepriteSrcDir, ct: ct);
            if (!checkoutResult.Success)
            {
                ctx.Log.Error($"git checkout {ctx.Options.GitRef} failed.");
                return false;
            }
        }

        // --- Update submodules ---
        ctx.Log.Info("Updating submodules...");
        var subResult = await runner.RunAsync(
            ctx.GitExe,
            "submodule update --init --recursive --depth 1",
            workingDir: ctx.AsepriteSrcDir, showOutput: true, ct: ct);

        if (!subResult.Success)
        {
            ctx.Log.Error("git submodule update failed.");
            return false;
        }

        // --- Verify ---
        var cmakelists = Path.Combine(ctx.AsepriteSrcDir, "CMakeLists.txt");
        if (!File.Exists(cmakelists))
        {
            ctx.Log.Error($"CMakeLists.txt not found in {ctx.AsepriteSrcDir} — clone may be incomplete.");
            return false;
        }

        ctx.Log.Success("Aseprite source ready.");
        return true;
    }

    public Task CleanupAsync(InstallContext ctx, CancellationToken ct = default)
    {
        // If clone failed and directory is incomplete, remove it.
        var cmakelists = Path.Combine(ctx.AsepriteSrcDir, "CMakeLists.txt");
        if (Directory.Exists(ctx.AsepriteSrcDir) && !File.Exists(cmakelists))
        {
            ctx.Log.Info("Cleaning up incomplete source clone.");
            PathUtils.DeleteDirectorySafe(ctx.AsepriteSrcDir);
        }
        return Task.CompletedTask;
    }
}
