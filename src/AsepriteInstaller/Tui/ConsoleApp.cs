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
        AnsiConsole.Write(new FigletText("Aseprite Installer")
            .Centered()
            .Color(Color.Cyan1));

        AnsiConsole.WriteLine();
        AnsiConsole.Write(new Panel(
            "[grey]Idempotent, atomic, one-click Aseprite build & install for Windows.[/]\n" +
            "[grey]This tool will download all dependencies, compile Aseprite from source,[/]\n" +
            "[grey]and install it to your chosen location.[/]")
            .RoundedBorder()
            .BorderColor(Color.Grey));
        AnsiConsole.WriteLine();
    }

    /// <summary>Display a summary of the installation plan before starting.</summary>
    public static void ShowPlan(InstallOptions opts)
    {
        var table = new Table().RoundedBorder().BorderColor(Color.Grey50);
        table.AddColumn(new TableColumn("Setting").LeftAligned());
        table.AddColumn(new TableColumn("Value").LeftAligned());

        table.AddRow("Install scope", opts.Scope == InstallScope.System ? "[yellow]System (requires admin)[/]" : "[green]User[/]");
        table.AddRow("Install directory", opts.InstallDir);
        table.AddRow("Working directory", opts.WorkDir);
        table.AddRow("VS handling", opts.VsMode.ToString());
        table.AddRow("Keep build artifacts", opts.KeepBuildArtifacts ? "Yes" : "No");
        table.AddRow("Create shortcut", opts.CreateShortcut ? "Yes" : "No");
        if (!string.IsNullOrEmpty(opts.GitRef))
            table.AddRow("Git ref", opts.GitRef);
        else
            table.AddRow("Git ref", "[grey]latest main[/]");

        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();
    }

    /// <summary>Display a final success summary.</summary>
    public static void ShowSuccess(string installDir)
    {
        AnsiConsole.WriteLine();
        AnsiConsole.Write(new Panel(
            $"[green]Aseprite has been successfully installed![/]\n\n" +
            $"Installation path: [cyan]{installDir}[/]\n\n" +
            $"You can now launch Aseprite from the Start Menu or directly from the installation path.")
            .RoundedBorder()
            .BorderColor(Color.Green));
    }

    /// <summary>Display an error panel with troubleshooting tips.</summary>
    public static void ShowError(string message, string? logPath = null)
    {
        AnsiConsole.WriteLine();
        var content = $"[red]{message}[/]";
        if (!string.IsNullOrEmpty(logPath))
            content += $"\n\n[grey]Detailed log: {logPath}[/]";
        AnsiConsole.Write(new Panel(content)
            .RoundedBorder()
            .BorderColor(Color.Red)
            .Header("[red] Installation Failed [/]"));
    }

    /// <summary>Display a step status table at the end.</summary>
    public static void ShowStepSummary(InstallState state)
    {
        var table = new Table().RoundedBorder().BorderColor(Color.Grey50);
        table.AddColumn("Step");
        table.AddColumn("Status");
        table.AddColumn("Duration / Message");

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
