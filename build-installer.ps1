<#
.SYNOPSIS
    Build the Aseprite Installer into a single native AOT executable.

.DESCRIPTION
    Runs `dotnet publish` with Native AOT to produce a single self-contained
    AsepriteInstaller.exe in the dist/ directory.

.PARAMETER Configuration
    Build configuration: Release (default) or Debug.

.EXAMPLE
    .\build-installer.ps1
    .\build-installer.ps1 -Configuration Debug
#>
param(
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectPath = Join-Path $scriptDir "src\AsepriteInstaller\AsepriteInstaller.csproj"
$distDir = Join-Path $scriptDir "dist"

Write-Host "Building Aseprite Installer ($Configuration)..." -ForegroundColor Cyan
Write-Host "Project: $projectPath"
Write-Host "Output:  $distDir"
Write-Host ""

# Clean previous output.
if (Test-Path $distDir) {
    Remove-Item $distDir -Recurse -Force
}
New-Item -ItemType Directory -Path $distDir -Force | Out-Null

# Publish with Native AOT.
# Note: The output path must not contain apostrophes due to an MSBuild path bug.
# We publish to a temp dir first, then copy to dist/.
$tempDist = Join-Path $env:TEMP "AsepriteInstaller-publish"
if (Test-Path $tempDist) { Remove-Item $tempDist -Recurse -Force }

dotnet publish $projectPath `
    -c $Configuration `
    -r win-x64 `
    -o $tempDist `
    /p:PublishAot=true

if ($LASTEXITCODE -ne 0) {
    Write-Host "Build FAILED." -ForegroundColor Red
    exit 1
}

# Copy results to dist/.
if (Test-Path $distDir) { Remove-Item $distDir -Recurse -Force }
New-Item -ItemType Directory -Path $distDir -Force | Out-Null
Copy-Item "$tempDist\AsepriteInstaller.exe" $distDir -Force
if (Test-Path "$tempDist\AsepriteInstaller.pdb") {
    Copy-Item "$tempDist\AsepriteInstaller.pdb" $distDir -Force
}

$exePath = Join-Path $distDir "AsepriteInstaller.exe"
if (Test-Path $exePath) {
    $size = (Get-Item $exePath).Length / 1MB
    Write-Host ""
    Write-Host "Build SUCCESS!" -ForegroundColor Green
    Write-Host "Output: $exePath ($($size.ToString('F1')) MB)" -ForegroundColor Green
} else {
    Write-Host "Build completed but AsepriteInstaller.exe not found in $distDir" -ForegroundColor Red
    exit 1
}
