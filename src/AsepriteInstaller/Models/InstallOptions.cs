namespace AsepriteInstaller.Models;

/// <summary>
/// Installation scope — system-wide or per-user.
/// </summary>
public enum InstallScope
{
    /// <summary>Install to C:\Program Files\Aseprite (requires admin/UAC).</summary>
    System,

    /// <summary>Install to %LOCALAPPDATA%\Programs\Aseprite (no admin needed).</summary>
    User,
}

/// <summary>
/// How to handle Visual Studio 2022 / Build Tools.
/// </summary>
public enum VsInstallMode
{
    /// <summary>Auto-install VS Build Tools silently (requires UAC).</summary>
    AutoInstall,

    /// <summary>Show instructions and download link, wait for user to install manually.</summary>
    Manual,

    /// <summary>VS is already installed — skip.</summary>
    Skip,
}

/// <summary>
/// All user-selectable options for a single installer run.
/// </summary>
public sealed class InstallOptions
{
    /// <summary>Where to install the final Aseprite binary.</summary>
    public InstallScope Scope { get; set; } = InstallScope.User;

    /// <summary>How to handle VS 2022 if it is not detected.</summary>
    public VsInstallMode VsMode { get; set; } = VsInstallMode.AutoInstall;

    /// <summary>Final installation directory (computed from Scope unless overridden).</summary>
    public string InstallDir { get; set; } = string.Empty;

    /// <summary>Working directory for tools, deps, source, build, staging.</summary>
    public string WorkDir { get; set; } = string.Empty;

    /// <summary>Keep build artifacts after installation (for faster future updates).</summary>
    public bool KeepBuildArtifacts { get; set; } = true;

    /// <summary>Create Start Menu shortcut.</summary>
    public bool CreateShortcut { get; set; } = true;

    /// <summary>Specific Aseprite git ref to build (empty = latest main).</summary>
    public string GitRef { get; set; } = string.Empty;

    /// <summary>Force re-run of all steps even if state.json says they are done.</summary>
    public bool Force { get; set; } = false;

    /// <summary>Internal flag: true if any option was set from command-line args.</summary>
    public bool _optionsSetFromArgs { get; set; } = false;

    /// <summary>Compute the default install directory from the scope.</summary>
    public static string DefaultInstallDir(InstallScope scope) =>
        scope == InstallScope.System
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Aseprite")
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "Aseprite");

    /// <summary>Default working directory under %LOCALAPPDATA%.</summary>
    public static string DefaultWorkDir() =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AsepriteInstaller");
}
