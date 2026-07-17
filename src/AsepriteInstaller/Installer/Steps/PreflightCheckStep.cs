using AsepriteInstaller.State;
using AsepriteInstaller.Utils;
using System.Runtime.InteropServices;

namespace AsepriteInstaller.Installer.Steps;

/// <summary>
/// Step 1: Pre-flight checks — OS version, architecture, disk space, network.
/// </summary>
public sealed class PreflightCheckStep : IInstallerStep
{
    public string StepId => "preflight";
    public string DisplayName => "Pre-flight checks";

    private const long MinDiskSpaceBytes = 5L * 1024 * 1024 * 1024; // 5 GB

    public Task<bool> ExecuteAsync(InstallContext ctx, CancellationToken ct = default)
    {
        // --- OS version ---
        var os = Environment.OSVersion;
        if (os.Platform != PlatformID.Win32NT)
        {
            ctx.Log.Error($"This installer requires Windows. Detected: {os.Platform}");
            return Task.FromResult(false);
        }

        // Windows 10 1809+ = build 17763+. Windows 11 = build 22000+.
        if (os.Version.Build < 17763)
        {
            ctx.Log.Error($"Windows 10 1809 (build 17763) or later required. Detected build {os.Version.Build}.");
            return Task.FromResult(false);
        }
        ctx.Log.Info($"OS: Windows build {os.Version.Build} ✓");

        // --- Architecture ---
        var arch = RuntimeInformation.OSArchitecture;
        if (arch != Architecture.X64 && arch != Architecture.Arm64)
        {
            ctx.Log.Error($"Only x64 and ARM64 architectures are supported. Detected: {arch}");
            return Task.FromResult(false);
        }
        ctx.Log.Info($"Architecture: {arch} ✓");

        // --- Disk space ---
        var workSpace = PathUtils.GetAvailableDiskSpace(ctx.Options.WorkDir);
        var installSpace = PathUtils.GetAvailableDiskSpace(ctx.Options.InstallDir);
        var minSpace = Math.Min(workSpace, installSpace);
        if (minSpace < MinDiskSpaceBytes)
        {
            ctx.Log.Error($"Insufficient disk space. Need ≥5 GB, available: {minSpace / 1024.0 / 1024 / 1024:F1} GB");
            return Task.FromResult(false);
        }
        ctx.Log.Info($"Disk space: {minSpace / 1024.0 / 1024 / 1024:F1} GB available ✓");

        // --- Network connectivity ---
        if (!CheckNetwork(ctx))
        {
            ctx.Log.Error("No network connection detected. This installer requires internet access.");
            return Task.FromResult(false);
        }
        ctx.Log.Info("Network: connected ✓");

        return Task.FromResult(true);
    }

    private static bool CheckNetwork(InstallContext ctx)
    {
        try
        {
            using var client = new System.Net.Http.HttpClient { Timeout = TimeSpan.FromSeconds(10) };
            var resp = client.GetAsync("https://github.com").GetAwaiter().GetResult();
            return resp.IsSuccessStatusCode || resp.StatusCode == System.Net.HttpStatusCode.Moved;
        }
        catch (Exception ex)
        {
            ctx.Log.Warn($"Network check failed: {ex.Message}");
            return false;
        }
    }
}
