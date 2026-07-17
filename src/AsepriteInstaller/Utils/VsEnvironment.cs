using System.Diagnostics;

namespace AsepriteInstaller.Utils;

/// <summary>
/// Captures the MSVC developer environment by running vcvars64.bat
/// and parsing the `set` command output.
/// </summary>
public static class VsEnvironment
{
    /// <summary>
    /// Run vcvars64.bat in a cmd subprocess, then dump all environment variables.
    /// Returns a dictionary of all env vars (including PATH, INCLUDE, LIB, etc.).
    /// </summary>
    public static Dictionary<string, string> Capture(string vcvars64Path, Logger log)
    {
        if (!File.Exists(vcvars64Path))
            throw new FileNotFoundException($"vcvars64.bat not found at {vcvars64Path}");

        log.Info($"Capturing MSVC environment from {vcvars64Path}");

        // Run: cmd /c ""vcvars64.bat" >nul 2>&1 && set"
        // The double quotes are tricky — cmd.exe requires nested quoting.
        var cmd = $"\"\"{vcvars64Path}\" >nul 2>&1 && set\"";

        var psi = new ProcessStartInfo
        {
            FileName = "cmd.exe",
            Arguments = $"/c {cmd}",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };

        using var p = Process.Start(psi)
            ?? throw new InvalidOperationException("Failed to start cmd.exe");
        var output = p.StandardOutput.ReadToEnd();
        p.WaitForExit(30000);

        if (p.ExitCode != 0)
        {
            var err = p.StandardError.ReadToEnd();
            throw new InvalidOperationException(
                $"vcvars64.bat failed with exit code {p.ExitCode}: {err}");
        }

        var env = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var line in output.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var idx = line.IndexOf('=');
            if (idx > 0)
            {
                var key = line[..idx].Trim();
                var val = line[(idx + 1)..].Trim();
                env[key] = val;
            }
        }

        log.Info($"Captured {env.Count} environment variables from MSVC dev environment");

        // Verify critical vars are present.
        if (!env.ContainsKey("INCLUDE") || !env.ContainsKey("LIB"))
        {
            log.Warn("MSVC environment is missing INCLUDE or LIB — VC tools may not be installed");
        }

        return env;
    }
}
