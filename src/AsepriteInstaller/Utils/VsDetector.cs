using System.Diagnostics;

namespace AsepriteInstaller.Utils;

/// <summary>
/// Detects installed Visual Studio 2022 / Build Tools using vswhere.exe.
/// vswhere.exe is installed alongside VS at a well-known path.
/// </summary>
public static class VsDetector
{
    private const string VswherePath =
        @"C:\Program Files (x86)\Microsoft Visual Studio\Installer\vswhere.exe";

    /// <summary>Information about a detected VS installation.</summary>
    public sealed class VsInstallation
    {
        public string InstallationPath { get; set; } = string.Empty;
        public string InstallationVersion { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public bool HasVCTools { get; set; }
        public bool IsBuildTools { get; set; }
    }

    /// <summary>
    /// Supported Visual Studio version ranges (2022 = v17, 2026 = v18).
    /// We check the newest first, then fall back to older versions.
    /// </summary>
    private static readonly string[] s_vsVersionRanges =
    {
        "[18.0,19.0)",  // VS 2026
        "[17.0,18.0)",  // VS 2022
    };

    /// <summary>
    /// Check if Visual Studio (2022 or 2026) with C++ tools is installed.
    /// Returns the installation info if found, null otherwise.
    /// </summary>
    public static VsInstallation? Detect()
    {
        if (!File.Exists(VswherePath))
            return null;

        // Check each supported version range, newest first.
        foreach (var range in s_vsVersionRanges)
        {
            // Find VS installation with VC++ tools.
            var result = RunVswhere(
                $"-prerelease -version \"{range}\" -products * " +
                "-requires Microsoft.VisualStudio.Component.VC.Tools.x86.x64 " +
                "-property installationPath");

            if (result.Success && !string.IsNullOrWhiteSpace(result.StdOut))
            {
                var installPath = result.StdOut.Trim();
                return BuildInstallInfo(installPath, range);
            }
        }

        // Fallback: find any supported VS without the VC requirement.
        foreach (var range in s_vsVersionRanges)
        {
            var result = RunVswhere(
                $"-version \"{range}\" -products * -property installationPath");

            if (result.Success && !string.IsNullOrWhiteSpace(result.StdOut))
            {
                var installPath = result.StdOut.Trim();
                var info = BuildInstallInfo(installPath, range);
                info.HasVCTools = false;
                return info;
            }
        }

        return null;
    }

    private static VsInstallation BuildInstallInfo(string installPath, string versionRange)
    {
        var info = new VsInstallation
        {
            InstallationPath = installPath,
            IsBuildTools = installPath.Contains("BuildTools", StringComparison.OrdinalIgnoreCase),
        };

        // Get version.
        var verResult = RunVswhere(
            $"-version \"{versionRange}\" -products * -property installationVersion");
        if (verResult.Success)
            info.InstallationVersion = verResult.StdOut.Trim();

        // Get display name.
        var nameResult = RunVswhere(
            $"-version \"{versionRange}\" -products * -property displayName");
        if (nameResult.Success)
            info.DisplayName = nameResult.StdOut.Trim();

        // Check for VC tools specifically.
        var vcResult = RunVswhere(
            $"-version \"{versionRange}\" -products * " +
            "-requires Microsoft.VisualStudio.Component.VC.Tools.x86.x64 " +
            "-property installationPath");
        info.HasVCTools = vcResult.Success &&
            vcResult.StdOut.Trim().Equals(installPath, StringComparison.OrdinalIgnoreCase);

        return info;
    }

    private static ProcessResult RunVswhere(string args)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = VswherePath,
                Arguments = args,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            };
            using var p = Process.Start(psi);
            if (p == null) return new ProcessResult { ExitCode = -1 };
            var stdout = p.StandardOutput.ReadToEnd();
            p.WaitForExit(5000);
            return new ProcessResult { ExitCode = p.ExitCode, StdOut = stdout };
        }
        catch
        {
            return new ProcessResult { ExitCode = -1 };
        }
    }

    /// <summary>Get the path to vcvars64.bat for a given VS installation.</summary>
    public static string GetVcvars64Path(string vsInstallPath) =>
        Path.Combine(vsInstallPath, "VC", "Auxiliary", "Build", "vcvars64.bat");
}
