using Spectre.Console;

namespace AsepriteInstaller.Utils;

/// <summary>
/// Dual-output logger: writes timestamped lines to both the console
/// (via Spectre.Console) and a log file.
/// </summary>
public sealed class Logger : IDisposable
{
    private readonly TextWriter _file;
    private readonly object _lock = new();
    public string LogFilePath { get; }

    public Logger(string logDir)
    {
        Directory.CreateDirectory(logDir);
        var stamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
        LogFilePath = Path.Combine(logDir, $"install-{stamp}.log");
        _file = new StreamWriter(LogFilePath, append: false) { AutoFlush = true };
    }

    public void Info(string message)
    {
        var line = $"[INFO ] {DateTime.Now:HH:mm:ss}  {message}";
        lock (_lock)
        {
            AnsiConsole.MarkupLine($"[grey]{DateTime.Now:HH:mm:ss}[/] {Escape(message)}");
            _file.WriteLine(line);
        }
    }

    public void Success(string message)
    {
        var line = $"[OK   ] {DateTime.Now:HH:mm:ss}  {message}";
        lock (_lock)
        {
            AnsiConsole.MarkupLine($"[green]✓[/] {Escape(message)}");
            _file.WriteLine(line);
        }
    }

    public void Warn(string message)
    {
        var line = $"[WARN ] {DateTime.Now:HH:mm:ss}  {message}";
        lock (_lock)
        {
            AnsiConsole.MarkupLine($"[yellow]⚠[/] {Escape(message)}");
            _file.WriteLine(line);
        }
    }

    public void Error(string message)
    {
        var line = $"[ERROR] {DateTime.Now:HH:mm:ss}  {message}";
        lock (_lock)
        {
            AnsiConsole.MarkupLine($"[red]✗[/] {Escape(message)}");
            _file.WriteLine(line);
        }
    }

    /// <summary>Escape [ and ] as [[ and ]] for Spectre.Console markup safety.</summary>
    private static string Escape(string text) => text.Replace("[", "[[").Replace("]", "]]");

    /// <summary>Log raw text without Spectre markup (for process output).</summary>
    public void Raw(string message)
    {
        lock (_lock)
        {
            _file.WriteLine(message);
        }
    }

    public void Dispose()
    {
        lock (_lock)
            _file.Dispose();
    }
}
