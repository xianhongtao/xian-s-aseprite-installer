using System.Diagnostics;
using AsepriteInstaller.Localization;
using AsepriteInstaller.Models;
using AsepriteInstaller.State;
using AsepriteInstaller.Tui;
using AsepriteInstaller.Utils;
using Spectre.Console;

namespace AsepriteInstaller.Installer.Steps;

/// <summary>
/// Step 2: Detect or install Visual Studio 2022 / Build Tools with C++ workload.
/// If VS is already installed with VC tools → skip.
/// If not installed → ask user (auto-install or manual) and act accordingly.
/// </summary>
public sealed class VisualStudioStep : IInstallerStep
{
    public string StepId => "visual-studio";
    public string DisplayName => Translations.StepVs;

    // VS 2022 (v17) Build Tools bootstrapper. This is the stable, well-tested version.
    // VS 2026 (v18) is also supported if already installed — detection handles both.
    private const string VsBuildToolsUrl = "https://aka.ms/vs/17/release/vs_buildtools.exe";

    /// <summary>
    /// This step is never skipped via state.json because the MSVC environment
    /// variables (ctx.VsEnv) are in-memory only and must be captured on every run.
    /// Instead, ExecuteAsync handles the idempotency internally by re-detecting VS.
    /// </summary>
    public bool CanSkip(InstallContext ctx) => false;

    public async Task<bool> ExecuteAsync(InstallContext ctx, CancellationToken ct = default)
    {
        // --- Detect existing VS installation ---
        var vs = VsDetector.Detect();

        if (vs != null && vs.HasVCTools)
        {
            ctx.Log.Info($"Found {vs.DisplayName} at {vs.InstallationPath}");
            ctx.Log.Info("VC++ tools detected ✓");
            ctx.VsInstallPath = vs.InstallationPath;

            // Capture MSVC environment.
            var vcvars = VsDetector.GetVcvars64Path(vs.InstallationPath);
            ctx.VsEnv = VsEnvironment.Capture(vcvars, ctx.Log);
            return true;
        }

        // If state says we already completed but VS is not detected now,
        // something changed — fall through to install logic.
        if (ctx.State.IsCompleted(StepId))
        {
            ctx.Log.Warn("VS was previously set up but is no longer detected. Re-checking...");
        }

        if (vs != null && !vs.HasVCTools)
        {
            ctx.Log.Warn($"Found {vs.DisplayName} but it lacks the C++ workload (VC Tools).");
            ctx.Log.Warn("The C++ workload will be installed.");
        }
        else
        {
            ctx.Log.Info("Visual Studio 2022 not found.");
        }

        // --- Handle based on user's chosen mode ---
        if (ctx.Options.VsMode == VsInstallMode.Manual)
        {
            return await HandleManualInstall(ctx, ct);
        }

        return await HandleAutoInstall(ctx, ct);
    }

    private async Task<bool> HandleAutoInstall(InstallContext ctx, CancellationToken ct)
    {
        ctx.Log.Info("Auto-installing Visual Studio Build Tools with C++ workload...");

        // Check if we have admin privileges.
        if (!IsRunningAsAdmin())
        {
            ctx.Log.Warn("Admin privileges required to install VS Build Tools.");
            ctx.Log.Warn("The installer will now relaunch with admin privileges.");
            ctx.Log.Warn("After VS is installed, re-run this installer to continue.");

            // Download the bootstrapper first.
            var downloader = new Downloader(ctx.Log);
            using (downloader)
            {
                var destPath = Path.Combine(ctx.Options.WorkDir, "vs_buildtools.exe");
                await downloader.DownloadAsync(VsBuildToolsUrl, destPath, "VS Build Tools bootstrapper", ct);

                // Launch with admin and wait.
                var psi = new ProcessStartInfo
                {
                    FileName = destPath,
                    Arguments = "--quiet --wait --norestart " +
                                "--add Microsoft.VisualStudio.Workload.VCTools " +
                                "--add Microsoft.VisualStudio.Component.Windows11SDK.26100 " +
                                "--includeRecommended",
                    UseShellExecute = true,
                    Verb = "runas", // Request UAC elevation.
                };

                try
                {
                    var p = Process.Start(psi);
                    if (p != null)
                    {
                        ctx.Log.Info("VS Build Tools installer launched with admin privileges. Waiting for completion...");
                        await p.WaitForExitAsync(ct);
                        if (p.ExitCode == 0 || p.ExitCode == 3010)
                        {
                            ctx.Log.Success("VS Build Tools installed successfully.");
                        }
                        else
                        {
                            ctx.Log.Error($"VS Build Tools installer exited with code {p.ExitCode}");
                            return false;
                        }
                    }
                }
                catch (System.ComponentModel.Win32Exception)
                {
                    ctx.Log.Error("UAC elevation was declined. Cannot install VS Build Tools.");
                    ctx.Log.Info("Please re-run the installer as administrator, or choose manual install.");
                    return false;
                }
            }
        }
        else
        {
            // We are admin — download and run directly.
            var downloader = new Downloader(ctx.Log);
            using (downloader)
            {
                var destPath = Path.Combine(ctx.Options.WorkDir, "vs_buildtools.exe");
                await downloader.DownloadAsync(VsBuildToolsUrl, destPath, "VS Build Tools bootstrapper", ct);

                var runner = new ProcessRunner(ctx.Log);
                var result = await runner.RunAsync(
                    destPath,
                    "--quiet --wait --norestart " +
                    "--add Microsoft.VisualStudio.Workload.VCTools " +
                    "--add Microsoft.VisualStudio.Component.Windows11SDK.26100 " +
                    "--includeRecommended",
                    showOutput: true, ct: ct);

                if (result.ExitCode != 0 && result.ExitCode != 3010)
                {
                    ctx.Log.Error($"VS Build Tools installer exited with code {result.ExitCode}");
                    return false;
                }
            }
        }

        // Re-detect VS after installation.
        ctx.Log.Info("Re-detecting Visual Studio installation...");
        var vs = VsDetector.Detect();
        if (vs == null || !vs.HasVCTools)
        {
            ctx.Log.Error("VS Build Tools installation completed but VC tools were not detected.");
            ctx.Log.Info("You may need to restart your computer and re-run this installer.");
            return false;
        }

        ctx.VsInstallPath = vs.InstallationPath;
        var vcvars = VsDetector.GetVcvars64Path(vs.InstallationPath);
        ctx.VsEnv = VsEnvironment.Capture(vcvars, ctx.Log);
        ctx.Log.Success("Visual Studio with C++ tools is ready.");
        return true;
    }

    private async Task<bool> HandleManualInstall(InstallContext ctx, CancellationToken ct)
    {
        AnsiConsole.WriteLine();
        AnsiConsole.Write(new Panel(
            Translations.PromptVsManualGuide)
            .RoundedBorder()
            .BorderColor(Color.Yellow));

        if (UserPrompts.ConfirmRetryAfterManualVs())
        {
            // Re-detect.
            var vs = VsDetector.Detect();
            if (vs != null && vs.HasVCTools)
            {
                ctx.VsInstallPath = vs.InstallationPath;
                var vcvars = VsDetector.GetVcvars64Path(vs.InstallationPath);
                ctx.VsEnv = VsEnvironment.Capture(vcvars, ctx.Log);
                ctx.Log.Success("Visual Studio with C++ tools detected.");
                return true;
            }

            ctx.Log.Error("Visual Studio with C++ tools still not detected.");
            return false;
        }

        return false;
    }

    private static bool IsRunningAsAdmin()
    {
        try
        {
            using var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
            var principal = new System.Security.Principal.WindowsPrincipal(identity);
            return principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
        }
        catch
        {
            return false;
        }
    }
}
