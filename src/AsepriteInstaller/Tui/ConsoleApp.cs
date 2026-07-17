using AsepriteInstaller.Localization;
using AsepriteInstaller.Models;
using AsepriteInstaller.State;
using Spectre.Console;

namespace AsepriteInstaller.Tui;

/// <summary>
/// High-level TUI helpers: welcome banner, step status display, error panels.
/// </summary>
public static class ConsoleApp
{
    /// <summary>Show the welcome banner and a brief description.</summary>
    public static void ShowBanner()
    {
        var title = Translations.CurrentLang == Language.ZhCN ? "Aseprite 安装器" : "Aseprite Installer";
        AnsiConsole.Write(new FigletText(title)
            .Centered()
            .Color(Color.Cyan1));

        AnsiConsole.WriteLine();
        AnsiConsole.Write(new Panel(
            $"[grey]{Translations.BannerDescription}[/]")
            .RoundedBorder()
            .BorderColor(Color.Grey));
        AnsiConsole.WriteLine();
    }

    /// <summary>Display a summary of the installation plan before starting.</summary>
    public static void ShowPlan(InstallOptions opts)
    {
        var table = new Table().RoundedBorder().BorderColor(Color.Grey50);
        table.AddColumn(new TableColumn(Translations.PlanSetting).LeftAligned());
        table.AddColumn(new TableColumn(Translations.PlanValue).LeftAligned());

        var scopeText = opts.Scope == InstallScope.System
            ? $"[yellow]{Translations.PlanScopeSystem}[/]"
            : $"[green]{Translations.PlanScopeUser}[/]";
        table.AddRow(Translations.PlanInstallScope, scopeText);
        table.AddRow(Translations.PlanInstallDir, opts.InstallDir);
        table.AddRow(Translations.PlanWorkDir, opts.WorkDir);
        table.AddRow(Translations.PlanVsHandling, opts.VsMode.ToString());
        table.AddRow(Translations.PlanKeepBuild, opts.KeepBuildArtifacts ? Translations.PlanYes : Translations.PlanNo);
        table.AddRow(Translations.PlanCreateShortcut, opts.CreateShortcut ? Translations.PlanYes : Translations.PlanNo);
        if (!string.IsNullOrEmpty(opts.GitRef))
            table.AddRow(Translations.PlanGitRef, opts.GitRef);
        else
            table.AddRow(Translations.PlanGitRef, $"[grey]{Translations.PlanLatestMain}[/]");

        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();
    }

    /// <summary>Display a final success summary.</summary>
    public static void ShowSuccess(string installDir)
    {
        AnsiConsole.WriteLine();
        AnsiConsole.Write(new Panel(
            $"[green]{Translations.InstallSuccess(installDir)}[/]")
            .RoundedBorder()
            .BorderColor(Color.Green));
    }

    /// <summary>Display an error panel with troubleshooting tips.</summary>
    public static void ShowError(string message, string? logPath = null)
    {
        AnsiConsole.WriteLine();
        var header = Translations.CurrentLang == Language.ZhCN ? "安装失败" : "Installation Failed";
        var content = $"[red]{message}[/]";
        if (!string.IsNullOrEmpty(logPath))
            content += $"\n\n[grey]{Translations.ErrorLogHint(logPath)}[/]";
        AnsiConsole.Write(new Panel(content)
            .RoundedBorder()
            .BorderColor(Color.Red)
            .Header($"[red] {header} [/]"));
    }

    /// <summary>Display a step status table at the end.</summary>
    public static void ShowStepSummary(InstallState state)
    {
        var table = new Table().RoundedBorder().BorderColor(Color.Grey50);
        table.AddColumn(Translations.SummaryStep);
        table.AddColumn(Translations.SummaryStatus);
        table.AddColumn(Translations.SummaryMessage);

        foreach (var s in state.Steps)
        {
            var status = s.Status switch
            {
                StepStatus.Completed => "[green]✓ Completed[/]",
                StepStatus.Skipped => "[grey]⊘ Skipped[/]",
                StepStatus.Failed => "[red]✗ Failed[/]",
                StepStatus.InProgress => "[yellow]… In Progress[/]",
                _ => "[grey]Pending[/]",
            };
            table.AddRow(s.StepId, status, s.Message ?? "");
        }

        AnsiConsole.Write(table);
    }
}
