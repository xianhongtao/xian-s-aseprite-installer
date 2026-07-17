using AsepriteInstaller.Models;

namespace AsepriteInstaller.Localization;

/// <summary>
/// All user-facing strings in both Chinese and English.
/// AOT-safe: simple static properties, no reflection.
/// </summary>
public static class Translations
{
    public static Language CurrentLang { get; set; } = Language.En;

    private static string _(string en, string zh) =>
        CurrentLang == Language.ZhCN ? zh : en;

    // ── Banner ──

    public static string BannerDescription =>
        _("Idempotent, atomic, one-click Aseprite build & install for Windows.\n" +
          "This tool will download all dependencies, compile Aseprite from source,\n" +
          "and install it to your chosen location.",
          "幂等、原子化、一键式 Aseprite Windows 编译安装工具。\n" +
          "本工具将自动下载所有依赖，从源码编译 Aseprite，\n" +
          "并安装到您指定的位置。");

    // ── Plan table ──

    public static string PlanSetting => _("Setting", "设置");
    public static string PlanValue => _("Value", "值");
    public static string PlanInstallScope => _("Install scope", "安装范围");
    public static string PlanInstallDir => _("Install directory", "安装目录");
    public static string PlanWorkDir => _("Working directory", "工作目录");
    public static string PlanVsHandling => _("VS handling", "VS 处理方式");
    public static string PlanKeepBuild => _("Keep build artifacts", "保留编译产物");
    public static string PlanCreateShortcut => _("Create shortcut", "创建快捷方式");
    public static string PlanGitRef => _("Git ref", "Git 版本");
    public static string PlanScopeSystem => _("System (requires admin)", "系统级（需要管理员权限）");
    public static string PlanScopeUser => _("User", "用户级");
    public static string PlanYes => _("Yes", "是");
    public static string PlanNo => _("No", "否");
    public static string PlanLatestMain => _("latest main", "最新的 main 分支");

    // ── Step summary ──

    public static string SummaryStep => _("Step", "步骤");
    public static string SummaryStatus => _("Status", "状态");
    public static string SummaryMessage => _("Duration / Message", "耗时 / 信息");

    // ── Success / Error ──

    public static string InstallSuccess(string path) =>
        _($"Aseprite has been successfully installed!\n\nInstallation path: {path}\n\nYou can now launch Aseprite from the Start Menu.",
          $"Aseprite 已成功安装！\n\n安装路径：{path}\n\n现在可以从开始菜单启动 Aseprite 了。");

    public static string InstallationFailed =>
        _("Installation did not complete. See the step summary above.",
          "安装未完成。请查看上方的步骤摘要。");

    public static string Cancelled =>
        _("Installation cancelled.", "安装已取消。");

    // ── User prompts ──

    public static string PromptScopeTitle =>
        _("Choose [cyan]installation scope[/]:", "选择[cyan]安装范围[/]：");
    public static string PromptScopeSystem =>
        _("System — C:\\Program Files\\Aseprite (requires admin)", "系统级 — C:\\Program Files\\Aseprite（需要管理员权限）");
    public static string PromptScopeUser =>
        _("User — %LOCALAPPDATA%\\Programs\\Aseprite (no admin needed)", "用户级 — %LOCALAPPDATA%\\Programs\\Aseprite（无需管理员）");

    public static string PromptVsTitle =>
        _("How should [cyan]Visual Studio 2022[/] be handled if not installed?",
          "如果未安装 [cyan]Visual Studio 2022[/]，应如何处理？");
    public static string PromptVsAuto =>
        _("Auto-install VS Build Tools (requires admin, ~3-5 GB)", "自动安装 VS Build Tools（需要管理员权限，约 3-5 GB）");
    public static string PromptVsManual =>
        _("Manual — show instructions, I'll install it myself", "手动 — 显示安装说明，我自己安装");

    public static string PromptKeepBuild =>
        _("Keep build artifacts after installation? (faster future updates, uses ~2 GB)",
          "安装后是否保留编译产物？（加快后续更新，占用约 2 GB）");

    public static string PromptShortcut =>
        _("Create a Start Menu shortcut?", "创建开始菜单快捷方式？");

    public static string PromptConfirmStart =>
        _("Ready to start the installation?", "准备好开始安装了吗？");

    public static string PromptVsManualGuide =>
        _("[yellow]Manual Visual Studio Installation[/]\n\n" +
          "Please download and install Visual Studio 2022 Community (or Build Tools):\n" +
          "  [link]https://aka.ms/vs/17/release/vs_buildtools.exe[/]\n\n" +
          "Required components:\n" +
          "  • [cyan]Desktop development with C++[/] workload\n" +
          "  • [cyan]Windows 11 SDK (10.0.26100.0)[/]\n" +
          "  • MSVC v143 - VS 2022 C++ x64/x86 build tools\n\n" +
          "After installation, re-run this installer.",
          "[yellow]手动安装 Visual Studio[/]\n\n" +
          "请下载并安装 Visual Studio 2022 Community（或 Build Tools）：\n" +
          "  [link]https://aka.ms/vs/17/release/vs_buildtools.exe[/]\n\n" +
          "需要安装的组件：\n" +
          "  • [cyan]使用 C++ 的桌面开发[/] 工作负载\n" +
          "  • [cyan]Windows 11 SDK (10.0.26100.0)[/]\n" +
          "  • MSVC v143 - VS 2022 C++ x64/x86 构建工具\n\n" +
          "安装完成后，重新运行此安装器。");

    public static string PromptVsRetry =>
        _("Press Enter after you have installed Visual Studio 2022 with C++ tools, to continue.",
          "安装完 Visual Studio 2022 含 C++ 工具后，按回车键继续。");

    public static string PromptCleanup =>
        _("Clean up build directory? (Keeping it speeds up future updates)",
          "是否清理构建目录？（保留可加快后续更新速度）");

    // ── UAC / System ──

    public static string UacRequired =>
        _("System-scope installation requires administrator privileges.",
          "系统级安装需要管理员权限。");
    public static string UacRelaunch =>
        _("Relaunching with UAC elevation...", "正在以管理员权限重新启动...");
    public static string UacDeclined =>
        _("UAC elevation was declined. Please run as administrator manually.",
          "UAC 提权被拒绝。请手动以管理员身份运行。");
    public static string UacNoExe =>
        _("Cannot determine executable path for UAC relaunch.",
          "无法确定可执行文件路径以重新提权启动。");

    // ── Language selection ──

    public static string PromptLanguage =>
        _("Select [cyan]language[/]:", "选择[cyan]语言[/]：");

    // ── Help text ──

    public static string HelpTitle => _("[bold]Aseprite Installer[/] — Usage", "[bold]Aseprite 安装器[/] — 使用说明");
    public static string HelpUsage => _("  AsepriteInstaller.exe [[options]]", "  AsepriteInstaller.exe [[选项]]");
    public static string HelpOptions => _("[bold]Options:[/]", "[bold]选项：[/]");
    public static string HelpSystem => _("  --system            Install to C:\\Program Files\\Aseprite (requires admin)", "  --system            安装到 C:\\Program Files\\Aseprite（需要管理员）");
    public static string HelpUser => _("  --user              Install to %LOCALAPPDATA%\\Programs\\Aseprite (default)", "  --user              安装到 %LOCALAPPDATA%\\Programs\\Aseprite（默认）");
    public static string HelpAutoVs => _("  --auto-vs           Auto-install VS Build Tools if missing (default)", "  --auto-vs           自动安装 VS Build Tools（默认）");
    public static string HelpManualVs => _("  --manual-vs         Show instructions for manual VS installation", "  --manual-vs         显示手动安装 VS 的说明");
    public static string HelpForce => _("  --force             Force re-run all steps (ignore state.json)", "  --force             强制重新运行所有步骤");
    public static string HelpNoShortcut => _("  --no-shortcut       Do not create Start Menu shortcut", "  --no-shortcut       不创建开始菜单快捷方式");
    public static string HelpNoKeepBuild => _("  --no-keep-build     Delete build directory after installation", "  --no-keep-build     安装后删除构建目录");
    public static string HelpVersion => _("  --version <ref>     Build specific Aseprite git ref (default: latest main)", "  --version <版本>    编译指定 Aseprite 版本（默认最新 main）");
    public static string HelpInstallDir => _("  --install-dir <p>   Custom installation directory", "  --install-dir <路径> 自定义安装目录");
    public static string HelpWorkDir => _("  --work-dir <p>      Custom working directory", "  --work-dir <路径>    自定义工作目录");
    public static string HelpMsg => _("  --help, -h          Show this help message", "  --help, -h          显示此帮助信息");

    // ── Some step DisplayNames ──

    public static string StepPreflight => _("Pre-flight checks", "环境检查");
    public static string StepVs => _("Visual Studio 2022 / Build Tools", "Visual Studio 2022 / 构建工具");
    public static string StepTools => _("Download build tools (CMake, Ninja, Git)", "下载构建工具（CMake, Ninja, Git）");
    public static string StepSkia => _("Download Skia prebuilt library", "下载 Skia 预编译库");
    public static string StepClone => _("Clone Aseprite source code", "克隆 Aseprite 源码");
    public static string StepBuild => _("Compile Aseprite", "编译 Aseprite");
    public static string StepInstall => _("Install Aseprite", "安装 Aseprite");
    public static string StepCleanup => _("Post-install cleanup", "安装后清理");

    // ── Various UI messages ──

    public static string ErrorLogHint(string path) =>
        _($"Detailed log: {path}", $"详细日志：{path}");
}
