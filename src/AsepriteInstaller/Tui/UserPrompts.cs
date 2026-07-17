using AsepriteInstaller.Localization;
using AsepriteInstaller.Models;
using Spectre.Console;

namespace AsepriteInstaller.Tui;

/// <summary>
/// Interactive user prompts for installation options.
/// </summary>
public static class UserPrompts
{
    /// <summary>Prompt user to select language first.</summary>
    public static Language PromptLanguage()
    {
        return AnsiConsole.Prompt(
            new SelectionPrompt<Language>()
                .Title(Translations.PromptLanguage)
                .PageSize(10)
                .AddChoices(Language.ZhCN, Language.En)
                .UseConverter(l => l.DisplayName()));
    }

    /// <summary>Ask the user for installation scope and other options.</summary>
    public static InstallOptions PromptOptions()
    {
        var scope = AnsiConsole.Prompt(
            new SelectionPrompt<InstallScope>()
                .Title(Translations.PromptScopeTitle)
                .PageSize(10)
                .AddChoices(InstallScope.User, InstallScope.System)
                .UseConverter(s => s == InstallScope.System
                    ? Translations.PromptScopeSystem
                    : Translations.PromptScopeUser));

        var vsMode = AnsiConsole.Prompt(
            new SelectionPrompt<VsInstallMode>()
                .Title(Translations.PromptVsTitle)
                .PageSize(10)
                .AddChoices(VsInstallMode.AutoInstall, VsInstallMode.Manual)
                .UseConverter(s => s switch
                {
                    VsInstallMode.AutoInstall => Translations.PromptVsAuto,
                    VsInstallMode.Manual => Translations.PromptVsManual,
                    _ => s.ToString(),
                }));

        var keepBuild = AnsiConsole.Confirm(
            Translations.PromptKeepBuild,
            defaultValue: true);

        var createShortcut = AnsiConsole.Confirm(
            Translations.PromptShortcut,
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
        return AnsiConsole.Confirm(Translations.PromptConfirmStart, defaultValue: true);
    }

    /// <summary>Ask whether to retry after a VS manual install.</summary>
    public static bool ConfirmRetryAfterManualVs()
    {
        AnsiConsole.WriteLine();
        return AnsiConsole.Confirm(
            Translations.PromptVsRetry,
            defaultValue: true);
    }

    /// <summary>Ask whether to clean up build files.</summary>
    public static bool ConfirmCleanup()
    {
        return AnsiConsole.Confirm(
            Translations.PromptCleanup,
            defaultValue: false);
    }
}
