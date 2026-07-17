using AsepriteInstaller.Models;
using Spectre.Console;

namespace AsepriteInstaller.Tui;

/// <summary>
/// Interactive user prompts for installation options.
/// </summary>
public static class UserPrompts
{
    /// <summary>Ask the user for installation scope and other options.</summary>
    public static InstallOptions PromptOptions()
    {
        var scope = AnsiConsole.Prompt(
            new SelectionPrompt<InstallScope>()
                .Title("Choose [cyan]installation scope[/]:")
                .PageSize(10)
                .AddChoices(InstallScope.User, InstallScope.System)
                .UseConverter(s => s == InstallScope.System
                    ? "System — C:\\Program Files\\Aseprite (requires admin)"
                    : "User — %LOCALAPPDATA%\\Programs\\Aseprite (no admin needed)"));

        var vsMode = AnsiConsole.Prompt(
            new SelectionPrompt<VsInstallMode>()
                .Title("How should [cyan]Visual Studio 2022[/] be handled if not installed?")
                .PageSize(10)
                .AddChoices(VsInstallMode.AutoInstall, VsInstallMode.Manual)
                .UseConverter(s => s switch
                {
                    VsInstallMode.AutoInstall => "Auto-install VS Build Tools (requires admin, ~3-5 GB)",
                    VsInstallMode.Manual => "Manual — show instructions, I'll install it myself",
                    _ => s.ToString(),
                }));

        var keepBuild = AnsiConsole.Confirm(
            "Keep build artifacts after installation? (faster future updates, uses ~2 GB)",
            defaultValue: true);

        var createShortcut = AnsiConsole.Confirm(
            "Create a Start Menu shortcut?",
            defaultValue: true);

        return new InstallOptions
        {
            Scope = scope,
            VsMode = vsMode,
            KeepBuildArtifacts = keepBuild,
            CreateShortcut = createShortcut,
        };
    }

    /// <summary>Ask the user to confirm before starting the installation.</summary>
    public static bool ConfirmStart()
    {
        return AnsiConsole.Confirm("Ready to start the installation?", defaultValue: true);
    }

    /// <summary>Ask whether to retry after a VS manual install.</summary>
    public static bool ConfirmRetryAfterManualVs()
    {
        AnsiConsole.WriteLine();
        return AnsiConsole.Confirm(
            "Press Enter after you have installed Visual Studio 2022 with C++ tools, to continue.",
            defaultValue: true);
    }

    /// <summary>Ask whether to clean up build files.</summary>
    public static bool ConfirmCleanup()
    {
        return AnsiConsole.Confirm(
            "Clean up build directory? (Keeping it speeds up future updates)",
            defaultValue: false);
    }
}
