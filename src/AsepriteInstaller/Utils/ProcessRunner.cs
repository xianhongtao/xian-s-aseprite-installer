using System.Diagnostics;

namespace AsepriteInstaller.Utils;

/// <summary>
/// Result of an external process execution.
/// </summary>
public sealed class ProcessResult
{
    public int ExitCode { get; init; }
    public string StdOut { get; init; } = string.Empty;
    public string StdErr { get; init; } = string.Empty;
    public bool Success => ExitCode == 0;
}

/// <summary>
/// Runs external processes with environment variable injection,
/// stdout/stderr capture, and optional real-time output display.
/// </summary>
public sealed class ProcessRunner
{
    private readonly Logger _log;

    public ProcessRunner(Logger log)
    {
        _log = log;
    }

    /// <summary>
    /// Run a process and capture all output. Returns when the process exits.
    /// </summary>
    public Task<ProcessResult> RunAsync(
        string fileName,
        string arguments,
        string? workingDir = null,
        Dictionary<string, string>? envVars = null,
        bool showOutput = false,
        CancellationToken ct = default)
    {
        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };

        if (!string.IsNullOrEmpty(workingDir))
            psi.WorkingDirectory = workingDir;

        if (envVars != null)
        {
            // Clear existing env and set all from the provided dictionary.
            // This is needed when we capture VS dev environment.
            foreach (var kv in envVars)
                psi.Environment[kv.Key] = kv.Value;
        }

        return RunCoreAsync(psi, showOutput, ct);
    }

    /// <summary>
    /// Run a process via cmd.exe with a command string (useful for batch files like vcvars64.bat).
    /// </summary>
    public Task<ProcessResult> RunCmdAsync(
        string command,
        string? workingDir = null,
        Dictionary<string, string>? envVars = null,
        bool showOutput = false,
        CancellationToken ct = default)
    {
        return RunAsync("cmd.exe", $"/c \"{command}\"", workingDir, envVars, showOutput, ct);
    }

    private async Task<ProcessResult> RunCoreAsync(ProcessStartInfo psi, bool showOutput, CancellationToken ct)
    {
        _log.Raw($"\n--- CMD: {psi.FileName} {psi.Arguments}\n  CWD: {psi.WorkingDirectory}\n---");

        using var p = new Process { StartInfo = psi, EnableRaisingEvents = true };

        var stdoutBuilder = new StringWriter();
        var stderrBuilder = new StringWriter();

        p.OutputDataReceived += (_, e) =>
        {
            if (e.Data == null) return;
            stdoutBuilder.WriteLine(e.Data);
            _log.Raw($"  {e.Data}");
            if (showOutput)
                Console.WriteLine(e.Data);
        };

        p.ErrorDataReceived += (_, e) =>
        {
            if (e.Data == null) return;
            stderrBuilder.WriteLine(e.Data);
            _log.Raw($"  [ERR] {e.Data}");
            if (showOutput)
                Console.Error.WriteLine(e.Data);
        };

        p.Start();
        p.BeginOutputReadLine();
        p.BeginErrorReadLine();

        await p.WaitForExitAsync(ct);

        var result = new ProcessResult
        {
            ExitCode = p.ExitCode,
            StdOut = stdoutBuilder.ToString(),
            StdErr = stderrBuilder.ToString(),
        };

        if (!result.Success)
            _log.Warn($"Process exited with code {p.ExitCode}: {psi.FileName}");

        return result;
    }
}
