<#
.SYNOPSIS
    Uninstall Aseprite (self-compiled) installed by xian's Aseprite Installer.
    卸载由 xian's Aseprite Installer 安装的 Aseprite (self-compiled)。

.DESCRIPTION
    Removes:
      - Installed Aseprite binaries and data
      - Start Menu shortcut "Aseprite (self-compiled)"
      - Optionally, the installer working directory (tools, source, build cache)

    Does NOT remove:
      - Visual Studio Build Tools (installed separately)
      - Portable tools downloaded to the working directory (unless --all is used)

.PARAMETER All
    Also remove the working directory (tools/skia/source/build cache).
    同时删除工作目录（工具/Skia/源码/构建缓存）。

.PARAMETER Quiet
    Quiet mode, skip confirmation prompts.
    静默模式，不询问确认。

.PARAMETER Lang
    Language: en (English) or zh-CN (中文). Auto-detected from system if omitted.
    语言：en（英文）或 zh-CN（中文）。默认根据系统自动检测。

.EXAMPLE
    .\uninstall.ps1                         # Interactive / 交互式
    .\uninstall.ps1 -Lang en                # English only
    .\uninstall.ps1 -All -Quiet             # Full silent uninstall
    .\uninstall.ps1 -All -Quiet -Lang zh-CN # 完全静默卸载（中文）
#>

param(
    [switch]$All,
    [switch]$Quiet,
    [string]$Lang = ""
)

$ErrorActionPreference = "Continue"

# ── Language detection ─────────────────────────────────────────────────────
if (-not $Lang) {
    $Lang = if ((Get-Culture).Name -eq 'zh-CN') { 'zh-CN' } else { 'en' }
}
$isCN = $Lang -eq 'zh-CN'

# ── i18n strings ─────────────────────────────────────────────────────────
function T {
    param([string]$en, [string]$zh)
    if ($isCN) { $zh } else { $en }
}

$str = @{
    AdminRequired   = $(T "System installation detected (Program Files). Administrator privileges required." "检测到系统级安装（Program Files），需要管理员权限。")
    AdminPlease     = $(T "Please re-run this script as administrator:" "请以管理员身份重新运行此脚本：")
    AdminCmd        = $(T "   sudo pwsh -NoProfile -File uninstall.ps1" "   sudo pwsh -NoProfile -File uninstall.ps1")
    AdminRightClick = $(T "   Or right-click PowerShell → 'Run as Administrator'" "   或在 PowerShell 中右键 →「以管理员身份运行」")
    Title           = $(T "`n xian's Aseprite Installer — Uninstall`n" "`n xian's Aseprite Installer — 卸载`n")
    NotFound        = $(T "No Aseprite (self-compiled) installation detected." "未检测到 Aseprite (self-compiled) 安装。")
    FoundInstall    = $(T "Found the following installation(s):" "检测到以下安装：")
    Incomplete      = $(T "(incomplete)" "(不完整)")
    ConfirmRemove   = $(T "Confirm uninstall Aseprite (self-compiled)?" "确认卸载 Aseprite (self-compiled)？")
    Cancelled       = $(T "Cancelled." "已取消。")
    Deleted         = $(T "deleted" "已删除")
    DeleteFailed    = $(T "failed to delete" "删除失败")
    NotFoundSkip    = $(T "not found, skipping" "不存在，跳过")
    Shortcut        = $(T "Shortcut" "快捷方式")
    ShortcutFolder  = $(T "Shortcut folder" "快捷方式文件夹")
    InstallDir      = $(T "Install directory" "安装目录")
    WorkDirKeep     = $(T "Working directory kept at:" "工作目录保留在：")
    WorkDirHint     = $(T "(contains portable tools/skia/source/build cache — speeds up next install)" "（包含便携工具/Skia/源码/构建缓存，下次安装可加速）")
    WorkDirAsk      = $(T "Remove working directory as well?" "是否也删除工作目录？")
    WorkDir         = $(T "Working directory" "工作目录")
    Done            = $(T "Uninstall complete." "卸载完成。")
    PurchaseHint    = $(T "Consider purchasing official Aseprite: https://aseprite.org" "建议使用官方 Aseprite：https://aseprite.org")
    YesNo           = $(T "[y/N]" "[y/N]")
}

# ── Self-elevation if admin is needed ──────────────────────────────────────
$systemPrograms = "${env:ProgramFiles}\Aseprite"
$needsAdmin = Test-Path $systemPrograms

if ($needsAdmin) {
    $identity = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = New-Object Security.Principal.WindowsPrincipal $identity
    $isAdmin = $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)

    if (-not $isAdmin) {
        Write-Host ""
        Write-Host "⚠️  $($str.AdminRequired)" -ForegroundColor Yellow
        Write-Host ""
        Write-Host "   $($str.AdminPlease)" -ForegroundColor Cyan
        Write-Host "   $($str.AdminCmd)" -ForegroundColor White
        Write-Host "   $($str.AdminRightClick)" -ForegroundColor White
        Write-Host ""
        exit 1
    }
}

# ── Paths ──────────────────────────────────────────────────────────────────
$userPrograms   = "$env:LOCALAPPDATA\Programs\Aseprite"
$workDir        = "$env:LOCALAPPDATA\AsepriteInstaller"
$shortcutDir    = "$env:APPDATA\Microsoft\Windows\Start Menu\Programs\Aseprite"
$shortcutPath   = "$shortcutDir\Aseprite (self-compiled).lnk"

# ── Helper functions ───────────────────────────────────────────────────────

function Write-Info  { Write-Host "ℹ️  $args" -ForegroundColor Cyan }
function Write-Ok    { Write-Host "✅ $args" -ForegroundColor Green }
function Write-Warn  { Write-Host "⚠️  $args" -ForegroundColor Yellow }
function Write-Error { Write-Host "❌ $args" -ForegroundColor Red }

function Confirm-Action {
    param([string]$Message)
    if ($Quiet) { return $true }
    $response = Read-Host "$Message $($str.YesNo)"
    return $response -eq 'y' -or $response -eq 'Y'
}

function Remove-ItemSafe {
    param([string]$Path, [string]$Label, [string]$Type)
    if (Test-Path $Path) {
        try {
            Remove-Item $Path -Recurse -Force -ErrorAction Stop
            Write-Ok "$Type $Label $($str.Deleted)"
        } catch {
            Write-Warn "$Type $Label $($str.DeleteFailed): $_"
        }
    } else {
        Write-Info "$Type $Label $($str.NotFoundSkip)"
    }
}

# ── Banner ─────────────────────────────────────────────────────────────────

Write-Host $str.Title -ForegroundColor Cyan

# ── Install dir detection ──────────────────────────────────────────────────

$installDirs = @()
if (Test-Path $userPrograms)  { $installDirs += $userPrograms }
if (Test-Path $systemPrograms) { $installDirs += $systemPrograms }

if ($installDirs.Count -eq 0) {
    Write-Info $str.NotFound
} else {
    Write-Info $str.FoundInstall
    foreach ($d in $installDirs) {
        $exe = Join-Path $d "aseprite.exe"
        if (Test-Path $exe) {
            $ver = (Get-Item $exe).VersionInfo.FileVersion
            Write-Info "  $d (v$ver)"
        } else {
            Write-Info "  $d $($str.Incomplete)"
        }
    }
    Write-Host ""

    if (-not (Confirm-Action $str.ConfirmRemove)) {
        Write-Info $str.Cancelled
        exit 0
    }

    foreach ($d in $installDirs) {
        Remove-ItemSafe -Path $d -Label $d -Type $str.InstallDir
    }
}

# ── Shortcut ───────────────────────────────────────────────────────────────

Remove-ItemSafe -Path $shortcutPath -Label $shortcutPath -Type $str.Shortcut
if ((Test-Path $shortcutDir) -and (-not (Get-ChildItem $shortcutDir -ErrorAction SilentlyContinue))) {
    Remove-ItemSafe -Path $shortcutDir -Label $shortcutDir -Type $str.ShortcutFolder
}

# ── Working directory ──────────────────────────────────────────────────────

if ($All) {
    Write-Host ""
    Remove-ItemSafe -Path $workDir -Label $workDir -Type $str.WorkDir
} else {
    Write-Host ""
    Write-Info "$($str.WorkDirKeep) $workDir"
    Write-Info "  $($str.WorkDirHint)"
    if ((Test-Path $workDir) -and (Confirm-Action $str.WorkDirAsk)) {
        Remove-ItemSafe -Path $workDir -Label $workDir -Type $str.WorkDir
    }
}

# ── Done ───────────────────────────────────────────────────────────────────

Write-Host ""
Write-Ok $str.Done
Write-Info $str.PurchaseHint
