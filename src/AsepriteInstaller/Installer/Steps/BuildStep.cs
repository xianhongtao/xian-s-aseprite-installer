using AsepriteInstaller.State;
using AsepriteInstaller.Utils;
using Spectre.Console;

namespace AsepriteInstaller.Installer.Steps;

/// <summary>
/// Step 6: Configure and build Aseprite using CMake + Ninja with the MSVC toolchain.
/// Captures the VS developer environment and passes it to the build processes.
/// Idempotent: if aseprite.exe already exists in the build output, it is skipped
/// (unless the source has been updated).
/// </summary>
public sealed class BuildStep : IInstallerStep
{
    public string StepId => "build";
    public string DisplayName => "Compile Aseprite";

    public async Task<bool> ExecuteAsync(InstallContext ctx, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(ctx.CMakeExe) || string.IsNullOrEmpty(ctx.NinjaExe))
        {
            ctx.Log.Error("Build tools not available. Run ToolsSetupStep first.");
            return false;
        }

        if (ctx.VsEnv.Count == 0)
        {
            ctx.Log.Error("MSVC environment not captured. Run VisualStudioStep first.");
            return false;
        }

        if (!Directory.Exists(ctx.AsepriteSrcDir))
        {
            ctx.Log.Error("Aseprite source not found. Run SourceCloneStep first.");
            return false;
        }

        // --- Check if already built ---
        var asepriteExe = Path.Combine(ctx.BuildDir, "bin", "aseprite.exe");
        if (File.Exists(asepriteExe) && !ctx.Options.Force)
        {
            ctx.Log.Info($"Aseprite already built at {asepriteExe} — skipping.");
            return true;
        }

        // --- Prepare build directory ---
        Directory.CreateDirectory(ctx.BuildDir);

        var runner = new ProcessRunner(ctx.Log);

        // --- CMake configure ---
        ctx.Log.Info("Configuring CMake...");

        // Convert paths to forward slashes for CMake compatibility.
        var skiaDir = ctx.SkiaDir.Replace('\\', '/');
        var skiaLibDir = ctx.SkiaLibDir.Replace('\\', '/');
        var ninjaExe = ctx.NinjaExe.Replace('\\', '/');

        var cmakeArgs =
            $"-G Ninja " +
            $"-DCMAKE_BUILD_TYPE=RelWithDebInfo " +
            $"-DLAF_BACKEND=skia " +
            $"-DSKIA_DIR=\"{skiaDir}\" " +
            $"-DSKIA_LIBRARY_DIR=\"{skiaLibDir}\" " +
            $"-DCMAKE_MAKE_PROGRAM=\"{ninjaExe}\" " +
            $"\"{ctx.AsepriteSrcDir.Replace('\\', '/')}\"";

        ctx.Log.Info($"cmake {cmakeArgs}");

        var cmakeResult = await runner.RunAsync(
            ctx.CMakeExe, cmakeArgs,
            workingDir: ctx.BuildDir,
            envVars: ctx.VsEnv,
            showOutput: true, ct: ct);

        if (!cmakeResult.Success)
        {
            ctx.Log.Error("CMake configuration failed.");
            return false;
        }

        ctx.Log.Success("CMake configuration complete.");

        // --- Ninja build ---
        ctx.Log.Info("Building Aseprite (this may take several minutes)...");

        // Run ninja with real-time output (no Status wrapper — it would conflict
        // with the process output being printed to the console).
        var ninjaResult = await runner.RunAsync(
                ctx.NinjaExe, "aseprite",
                workingDir: ctx.BuildDir,
                envVars: ctx.VsEnv,
                showOutput: true, ct: ct);

        if (!ninjaResult.Success)
        {
            ctx.Log.Error($"Ninja build failed with exit code {ninjaResult.ExitCode}");
            return false;
        }

        // --- Verify ---
        if (!File.Exists(asepriteExe))
        {
            ctx.Log.Error($"Build completed but aseprite.exe not found at {asepriteExe}");
            return false;
        }

        var size = new FileInfo(asepriteExe).Length;
        ctx.Log.Success($"Aseprite built successfully ({size / 1024.0 / 1024.0:F1} MB): {asepriteExe}");
        return true;
    }

    public Task CleanupAsync(InstallContext ctx, CancellationToken ct = default)
    {
        // On build failure, clean the build directory to allow a fresh retry.
        if (Directory.Exists(ctx.BuildDir))
        {
            ctx.Log.Info("Cleaning build directory for fresh retry.");
            PathUtils.DeleteDirectorySafe(ctx.BuildDir);
        }
        return Task.CompletedTask;
    }
}
