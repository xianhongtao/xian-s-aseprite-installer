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
    同时删除工作目录（工具/Skia/源码/构建缓存）。
    Also remove the working directory (tools/skia/source/build cache).

.PARAMETER Quiet
    静默模式，不询问确认。
    Quiet mode, skip confirmation prompts.

.EXAMPLE
    .\uninstall.ps1                  # 交互式卸载
    .\uninstall.ps1 -All -Quiet      # 完全静默卸载（含工作目录）
#>

param(
    [switch]$All,
    [switch]$Quiet
)

$ErrorActionPreference = "Continue"

# ── Paths ──────────────────────────────────────────────────────────────────
# These match the defaults used by xian's Aseprite Installer.

$userPrograms   = "$env:LOCALAPPDATA\Programs\Aseprite"
$systemPrograms = "${env:ProgramFiles}\Aseprite"
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
    $response = Read-Host "$Message [y/N]"
    return $response -eq 'y' -or $response -eq 'Y'
}

function Remove-ItemSafe {
    param([string]$Path, [string]$Label)
    if (Test-Path $Path) {
        try {
            Remove-Item $Path -Recurse -Force -ErrorAction Stop
            Write-Ok "$Label 已删除"
        } catch {
            Write-Warn "$Label 删除失败: $_"
        }
    } else {
        Write-Info "$Label 不存在，跳过"
    }
}

# ── Banner ─────────────────────────────────────────────────────────────────

Write-Host @"

 xian's Aseprite Installer — Uninstall / 卸载

"@ -ForegroundColor Cyan

# ── Install dir detection ──────────────────────────────────────────────────

$installDirs = @()
if (Test-Path $userPrograms)  { $installDirs += $userPrograms }
if (Test-Path $systemPrograms) { $installDirs += $systemPrograms }

if ($installDirs.Count -eq 0) {
    Write-Info "未检测到 Aseprite (self-compiled) 安装。"
}
else {
    Write-Info "检测到以下安装："
    foreach ($d in $installDirs) {
        $exe = Join-Path $d "aseprite.exe"
        if (Test-Path $exe) {
            $ver = (Get-Item $exe).VersionInfo.FileVersion
            Write-Info "  $d (v$ver)"
        } else {
            Write-Info "  $d (不完整)"
        }
    }
    Write-Host ""

    if (-not (Confirm-Action "确认卸载 Aseprite (self-compiled)？")) {
        Write-Info "已取消。"
        exit 0
    }

    # Remove install directories
    foreach ($d in $installDirs) {
        Remove-ItemSafe -Path $d -Label "安装目录 $d"
    }
}

# ── Shortcut ───────────────────────────────────────────────────────────────

Remove-ItemSafe -Path $shortcutPath -Label "快捷方式 $shortcutPath"
# Remove folder if empty
if (Test-Path $shortcutDir -and -not (Get-ChildItem $shortcutDir -ErrorAction SilentlyContinue)) {
    Remove-ItemSafe -Path $shortcutDir -Label "快捷方式文件夹 $shortcutDir"
}

# ── Working directory ──────────────────────────────────────────────────────

if ($All) {
    Write-Host ""
    Remove-ItemSafe -Path $workDir -Label "工作目录 $workDir"
} else {
    Write-Host ""
    Write-Info "工作目录保留在: $workDir"
    Write-Info "（包含便携工具/Skia/源码/构建缓存，下次安装可加速）"
    if ((Test-Path $workDir) -and (Confirm-Action "是否也删除工作目录？")) {
        Remove-ItemSafe -Path $workDir -Label "工作目录 $workDir"
    }
}

# ── Done ───────────────────────────────────────────────────────────────────

Write-Host ""
Write-Ok "卸载完成。"
Write-Info "建议使用官方 Aseprite: https://aseprite.org"
