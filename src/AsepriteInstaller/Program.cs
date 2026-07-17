using System.Diagnostics;
using AsepriteInstaller.Installer;
using AsepriteInstaller.Installer.Steps;
using AsepriteInstaller.Localization;
using AsepriteInstaller.Models;
using AsepriteInstaller.State;
using AsepriteInstaller.Tui;
using AsepriteInstaller.Utils;
using Spectre.Console;

namespace AsepriteInstaller;

/// <summary>
/// Entry point for the Aseprite Installer.
/// Handles UAC elevation, user prompts, and orchestrates the installation pipeline.
/// </summary>
public static class Program
{
    /// <summary>True if --lang was specified via command line args.</summary>
    private static bool s_langSetFromArgs;
    private static bool s_eulaAccepted;
    private static InstallOptions s_options = new();

    public static async Task<int> Main(string[] args)
    {
        // Parse simple command-line args.
        s_options = ParseArgs(args);

        // ── Step 0: Language selection ──
        if (!s_langSetFromArgs)
            Translations.CurrentLang = UserPrompts.PromptLanguage();

        // Show banner.
        ConsoleApp.ShowBanner();

        // ── EULA acceptance ──
        if (!s_eulaAccepted)
        {
            if (!UserPrompts.PromptEulaAccept())
            {
                AnsiConsole.MarkupLine($"[red]{Translations.EulaRejected}[/]");
                return 1;
            }
        }

        // ── Interactive options (scope, install dir, VS, etc.) ──
        // Do this BEFORE UAC elevation so the user's choices are captured.
        if (!s_options._optionsSetFromArgs)
        {
            var prompted = UserPrompts.PromptOptions();
            prompted._optionsSetFromArgs = true;
            s_options = prompted;
        }

        // Resolve default paths.
        if (string.IsNullOrEmpty(s_options.WorkDir))
            s_options.WorkDir = InstallOptions.DefaultWorkDir();
        if (string.IsNullOrEmpty(s_options.InstallDir))
            s_options.InstallDir = InstallOptions.DefaultInstallDir(s_options.Scope);

        // ── UAC elevation for system scope ──
        // After all prompts, so the relaunched admin process can skip prompts.
        if (s_options.Scope == InstallScope.System && !IsRunningAsAdmin())
        {
            AnsiConsole.MarkupLine($"[yellow]{Translations.UacRequired}[/]");
            AnsiConsole.MarkupLine($"[yellow]{Translations.UacRelaunch}[/]");
            Thread.Sleep(1500);
            RelaunchAsAdmin(s_options);
            return 0;
        }

        // Show the plan.
        ConsoleApp.ShowPlan(s_options);

        if (!UserPrompts.ConfirmStart())
        {
            AnsiConsole.MarkupLine($"[grey]{Translations.Cancelled}[/]");
            return 0;
        }

        // --- Initialize context ---
        var statePath = Path.Combine(s_options.WorkDir, "state.json");
        var state = InstallState.LoadOrCreate(statePath);
        using var logger = new Logger(Path.Combine(s_options.WorkDir, "logs"));
        var ctx = InstallContext.Create(s_options, state, logger);

        ctx.Log.Info($"=== xian's Aseprite Installer started ===");
        ctx.Log.Info($"Work dir: {s_options.WorkDir}");
        ctx.Log.Info($"Install dir: {s_options.InstallDir}");

        // --- Build the step pipeline with localized names ---
        var steps = new IInstallerStep[]
        {
            new PreflightCheckStep(),
            new VisualStudioStep(),
            new ToolsSetupStep(),
            new SkiaSetupStep(),
            new SourceCloneStep(),
            new BuildStep(),
            new InstallStep(),
            new CleanupStep(),
        };

        var engine = new InstallerEngine(ctx, steps);

        // --- Run ---
        using var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            cts.Cancel();
            ctx.Log.Warn("Cancellation requested...");
        };

        bool success;
        try
        {
            success = await engine.RunAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            ctx.Log.Warn("Installation was cancelled.");
            success = false;
        }

        // --- Show summary ---
        AnsiConsole.WriteLine();
        ConsoleApp.ShowStepSummary(state);

        if (success)
        {
            ConsoleApp.ShowSuccess(s_options.InstallDir);
            ctx.Log.Info("=== Installation completed successfully ===");
            return 0;
        }
        else
        {
            ConsoleApp.ShowError(Translations.InstallationFailed, logger.LogFilePath);
            ctx.Log.Info("=== Installation failed ===");
            return 1;
        }
    }

    // ------------------------------------------------------------------
    //  UAC elevation
    // ------------------------------------------------------------------

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

    private static void RelaunchAsAdmin(InstallOptions opts)
    {
        var exePath = Environment.ProcessPath ?? Process.GetCurrentProcess().MainModule?.FileName;
        if (string.IsNullOrEmpty(exePath))
        {
            AnsiConsole.MarkupLine($"[red]{Translations.UacNoExe}[/]");
            return;
        }

        // Serialize all user choices into CLI args so the elevated process
        // can skip prompts and proceed directly.
        var args = new List<string>
        {
            $"--lang {Translations.CurrentLang.ToCode()}",
            "--eula-accepted",
            opts.Scope == InstallScope.System ? "--system" : "--user",
            opts.VsMode == VsInstallMode.AutoInstall ? "--auto-vs" : "--manual-vs",
            opts.KeepBuildArtifacts ? "" : "--no-keep-build",
            opts.CreateShortcut ? "" : "--no-shortcut",
        };

        if (!string.IsNullOrEmpty(opts.InstallDir) &&
            opts.InstallDir != InstallOptions.DefaultInstallDir(opts.Scope))
            args.Add($"--install-dir \"{opts.InstallDir}\"");

        if (!string.IsNullOrEmpty(opts.GitRef))
            args.Add($"--version {opts.GitRef}");

        var argStr = string.Join(' ', args.Where(a => !string.IsNullOrEmpty(a)));

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = exePath,
                Arguments = argStr,
                UseShellExecute = true,
                Verb = "runas",
            };
            Process.Start(psi);
        }
        catch (System.ComponentModel.Win32Exception)
        {
            AnsiConsole.MarkupLine($"[red]{Translations.UacDeclined}[/]");
        }
    }

    // ------------------------------------------------------------------
    //  Argument parsing
    // ------------------------------------------------------------------

    private static InstallOptions ParseArgs(string[] args)
    {
        var opts = new InstallOptions();

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i].ToLowerInvariant())
            {
                case "--system":
                    opts.Scope = InstallScope.System;
                    opts._optionsSetFromArgs = true;
                    break;
                case "--user":
                    opts.Scope = InstallScope.User;
                    opts._optionsSetFromArgs = true;
                    break;
                case "--auto-vs":
                    opts.VsMode = VsInstallMode.AutoInstall;
                    break;
                case "--manual-vs":
                    opts.VsMode = VsInstallMode.Manual;
                    break;
                case "--force":
                    opts.Force = true;
                    break;
                case "--no-shortcut":
                    opts.CreateShortcut = false;
                    break;
                case "--no-keep-build":
                    opts.KeepBuildArtifacts = false;
                    break;
                case "--version":
                case "--ref":
                    if (i + 1 < args.Length)
                    {
                        opts.GitRef = args[++i];
                        opts._optionsSetFromArgs = true;
                    }
                    break;
                case "--lang":
                    if (i + 1 < args.Length)
                    {
                        Translations.CurrentLang = LanguageExtensions.FromCode(args[++i]);
                        s_langSetFromArgs = true;
                    }
                    break;
                case "--eula-accepted":
                    s_eulaAccepted = true;
                    break;
                case "--install-dir":
                    if (i + 1 < args.Length)
                    {
                        opts.InstallDir = args[++i];
                        opts._optionsSetFromArgs = true;
                    }
                    break;
                case "--work-dir":
                    if (i + 1 < args.Length)
                    {
                        opts.WorkDir = args[++i];
                        opts._optionsSetFromArgs = true;
                    }
                    break;
                case "--help":
                case "-h":
                    ShowHelp();
                    Environment.Exit(0);
                    break;
            }
        }

        return opts;
    }

    private static void ShowHelp()
    {
        AnsiConsole.Write(new Panel(
            $"{Translations.HelpTitle}\n\n" +
            $"{Translations.HelpUsage}\n\n" +
            $"{Translations.HelpOptions}\n" +
            $"{Translations.HelpSystem}\n" +
            $"{Translations.HelpUser}\n" +
            $"{Translations.HelpAutoVs}\n" +
            $"{Translations.HelpManualVs}\n" +
            $"{Translations.HelpForce}\n" +
            $"{Translations.HelpNoShortcut}\n" +
            $"{Translations.HelpNoKeepBuild}\n" +
            $"{Translations.HelpVersion}\n" +
            $"{Translations.HelpInstallDir}\n" +
            $"{Translations.HelpWorkDir}\n" +
            $"{Translations.HelpLang}\n" +
            $"{Translations.HelpMsg}")
            .RoundedBorder()
            .BorderColor(Color.Cyan1));
    }
}
