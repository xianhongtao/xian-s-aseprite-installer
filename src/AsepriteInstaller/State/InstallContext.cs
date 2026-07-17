using AsepriteInstaller.Models;
using AsepriteInstaller.Utils;

namespace AsepriteInstaller.State;

/// <summary>
/// Global context shared by all installer steps.
/// Holds resolved paths, options, logger, and persistent state.
/// </summary>
public sealed class InstallContext
{
    public InstallOptions Options { get; }
    public InstallState State { get; }
    public Logger Log { get; }

    // Resolved working-directory sub-paths.
    public string ToolsDir { get; }
    public string DepsDir { get; }
    public string SkiaDir { get; }
    public string SrcDir { get; }
    public string AsepriteSrcDir { get; }
    public string BuildDir { get; }
    public string StagingDir { get; }
    public string LogsDir { get; }

    // Resolved tool paths (filled in by ToolsSetupStep).
    public string CMakeExe { get; set; } = string.Empty;
    public string NinjaExe { get; set; } = string.Empty;
    public string GitExe { get; set; } = string.Empty;

    // Skia info (filled in by SkiaSetupStep).
    public string SkiaTag { get; set; } = string.Empty;
    public string SkiaLibDir { get; set; } = string.Empty;

    // VS info (filled in by VisualStudioStep).
    public string VsInstallPath { get; set; } = string.Empty;
    public Dictionary<string, string> VsEnv { get; set; } = [];

    private InstallContext(InstallOptions opts, InstallState state, Logger log)
    {
        Options = opts;
        State = state;
        Log = log;

        if (string.IsNullOrEmpty(opts.WorkDir))
            opts.WorkDir = InstallOptions.DefaultWorkDir();
        if (string.IsNullOrEmpty(opts.InstallDir))
            opts.InstallDir = InstallOptions.DefaultInstallDir(opts.Scope);

        ToolsDir = Path.Combine(opts.WorkDir, "tools");
        DepsDir = Path.Combine(opts.WorkDir, "deps");
        SkiaDir = Path.Combine(DepsDir, "skia");
        SrcDir = Path.Combine(opts.WorkDir, "src");
        AsepriteSrcDir = Path.Combine(SrcDir, "aseprite");
        BuildDir = Path.Combine(opts.WorkDir, "build");
        StagingDir = Path.Combine(opts.WorkDir, "staging");
        LogsDir = Path.Combine(opts.WorkDir, "logs");
    }

    /// <summary>Create a context, ensuring all working directories exist.</summary>
    public static InstallContext Create(InstallOptions opts, InstallState state, Logger log)
    {
        var ctx = new InstallContext(opts, state, log);
        foreach (var d in new[] { opts.WorkDir, ctx.ToolsDir, ctx.DepsDir, ctx.SrcDir, ctx.StagingDir, ctx.LogsDir })
            Directory.CreateDirectory(d);
        return ctx;
    }

    /// <summary>Compute a simple checksum (file size + last-write-time) for a path.</summary>
    public static string PathChecksum(string path)
    {
        if (!File.Exists(path) && !Directory.Exists(path))
            return string.Empty;
        var info = new DirectoryInfo(path);
        long size = 0;
        if (File.Exists(path))
            size = new FileInfo(path).Length;
        else
            size = info.EnumerateFiles("*", SearchOption.AllDirectories).Sum(f => f.Length);
        var ts = File.GetLastWriteTimeUtc(path).Ticks;
        return $"{size:X16}-{ts:X16}";
    }
}
