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

    public static async Task<int> Main(string[] args)
    {
        // Parse simple command-line args.
        var opts = ParseArgs(args);

        // ── Step 0: Language selection (before anything else) ──
        if (!s_langSetFromArgs)
            Translations.CurrentLang = UserPrompts.PromptLanguage();

        // Show banner.
        ConsoleApp.ShowBanner();

        // Handle UAC elevation for system-scope install.
        if (opts.Scope == InstallScope.System && !IsRunningAsAdmin())
        {
            AnsiConsole.MarkupLine($"[yellow]{Translations.UacRequired}[/]");
            AnsiConsole.MarkupLine($"[yellow]{Translations.UacRelaunch}[/]");
            Thread.Sleep(1500);
            RelaunchAsAdmin(args);
            return 0; // Never reached if relaunch succeeds.
        }

        // Prompt for options if not fully specified via args.
        if (!opts._optionsSetFromArgs)
        {
            var prompted = UserPrompts.PromptOptions();
            prompted.Scope = opts.Scope; // Keep scope from args if set.
            if (opts.Scope == InstallScope.System)
                prompted.Scope = InstallScope.System;
            opts = prompted;
        }

        // Resolve default paths.
        if (string.IsNullOrEmpty(opts.WorkDir))
            opts.WorkDir = InstallOptions.DefaultWorkDir();
        if (string.IsNullOrEmpty(opts.InstallDir))
            opts.InstallDir = InstallOptions.DefaultInstallDir(opts.Scope);

        // Show the plan.
        ConsoleApp.ShowPlan(opts);

        if (!UserPrompts.ConfirmStart())
        {
            AnsiConsole.MarkupLine($"[grey]{Translations.Cancelled}[/]");
            return 0;
        }

        // --- Initialize context ---
        var statePath = Path.Combine(opts.WorkDir, "state.json");
        var state = InstallState.LoadOrCreate(statePath);
        using var logger = new Logger(Path.Combine(opts.WorkDir, "logs"));
        var ctx = InstallContext.Create(opts, state, logger);

        ctx.Log.Info($"=== Aseprite Installer started ===");
        ctx.Log.Info($"Work dir: {opts.WorkDir}");
        ctx.Log.Info($"Install dir: {opts.InstallDir}");

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
            ConsoleApp.ShowSuccess(opts.InstallDir);
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

    private static void RelaunchAsAdmin(string[] args)
    {
        var exePath = Environment.ProcessPath ?? Process.GetCurrentProcess().MainModule?.FileName;
        if (string.IsNullOrEmpty(exePath))
        {
            AnsiConsole.MarkupLine($"[red]{Translations.UacNoExe}[/]");
            return;
        }

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = exePath,
                Arguments = string.Join(' ', args),
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
