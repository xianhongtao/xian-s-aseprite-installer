# Aseprite Installer

A **one-click, idempotent, atomic** installer for [Aseprite](https://github.com/aseprite/aseprite) on Windows.

Double-click the single `AsepriteInstaller.exe` and it will:

1. **Detect / install** Visual Studio 2022 Build Tools with C++ workload
2. **Download** portable CMake, Ninja, and Git (no system pollution)
3. **Download** the prebuilt Skia library matching Aseprite's requirements
4. **Clone** the Aseprite source code with all submodules
5. **Compile** Aseprite from source using CMake + Ninja + MSVC
6. **Install** the finished binary to your chosen location (atomically)
7. **Create** a Start Menu shortcut

## Quick Start

```powershell
# Build the installer
.\build-installer.ps1

# Run it (double-click dist\AsepriteInstaller.exe or:)
.\dist\AsepriteInstaller.exe
```

## Features

- **Single file**: One self-contained `.exe` (~10 MB), no .NET runtime needed
- **Idempotent**: Re-running skips already-completed steps; safe to interrupt and resume
- **Atomic**: Failed installs never corrupt an existing Aseprite installation
- **No system pollution**: Build tools are downloaded to a working directory, not installed system-wide
- **User or System scope**: Install per-user (no admin) or system-wide (UAC prompt)
- **TUI interface**: Interactive console UI with progress bars and colored output

## Command-Line Options

```
AsepriteInstaller.exe [options]

  --system            Install to C:\Program Files\Aseprite (requires admin)
  --user              Install to %LOCALAPPDATA%\Programs\Aseprite (default)
  --auto-vs           Auto-install VS Build Tools if missing (default)
  --manual-vs         Show instructions for manual VS installation
  --force             Force re-run all steps (ignore saved state)
  --no-shortcut       Do not create Start Menu shortcut
  --no-keep-build     Delete build directory after installation
  --version <ref>     Build specific Aseprite git ref (default: latest main)
  --install-dir <p>   Custom installation directory
  --work-dir <p>      Custom working directory
  --help, -h          Show help
```

## How It Works

### Working Directory

All downloads, source, and build artifacts are stored in:
```
%LOCALAPPDATA%\AsepriteInstaller\
├── tools/         # Portable CMake, Ninja, MinGit
├── deps/skia/     # Prebuilt Skia library
├── src/aseprite/  # Aseprite source (git clone)
├── build/         # CMake build output
├── staging/       # Atomic install staging area
├── logs/          # Installation logs
└── state.json     # Idempotency state tracking
```

### Idempotency

Each step's completion is recorded in `state.json`. Re-running the installer reads this file and skips steps that have already completed successfully. Use `--force` to ignore the state and re-run everything.

### Atomic Installation

Before installing, the built binary is staged in a temporary directory. Only after verification does it atomically replace the existing installation. If the swap fails, the previous version is restored from backup.

### Dependencies

The installer handles everything automatically:

| Dependency | How it's handled |
|---|---|
| Visual Studio 2022 + C++ | Detected via `vswhere`; auto-installed (Build Tools) or manual |
| CMake ≥ 3.20 | Portable ZIP downloaded to working directory |
| Ninja | Portable ZIP downloaded to working directory |
| Git | MinGit portable ZIP downloaded to working directory |
| Skia | Prebuilt release ZIP from `aseprite/skia` GitHub releases |
| Aseprite source | `git clone --recursive` from `aseprite/aseprite` |
| Third-party libs | Built automatically from Aseprite's git submodules |

## Building the Installer

### Prerequisites

- .NET 10 SDK (or .NET 8+ with Native AOT support)
- Visual Studio 2022 with C++ workload (for AOT compilation of the installer itself)

### Build

```powershell
.\build-installer.ps1
```

Output: `dist/AsepriteInstaller.exe`

## Project Structure

```
src/AsepriteInstaller/
├── AsepriteInstaller.csproj    # .NET Native AOT project config
├── Program.cs                  # Entry point, UAC, arg parsing
├── Models/
│   └── InstallOptions.cs       # User options model
├── State/
│   ├── InstallState.cs         # state.json idempotency tracking
│   └── InstallContext.cs       # Global context (paths, tools, env)
├── Utils/
│   ├── Logger.cs               # Dual console + file logger
│   ├── Downloader.cs           # HTTP download with progress + retry
│   ├── ArchiveExtractor.cs     # ZIP extraction
│   ├── ProcessRunner.cs        # External process execution
│   ├── VsDetector.cs           # VS 2022 detection via vswhere
│   ├── VsEnvironment.cs        # MSVC env capture (vcvars64.bat)
│   └── PathUtils.cs            # Filesystem helpers
├── Tui/
│   ├── ConsoleApp.cs           # Banner, plan, summary panels
│   └── UserPrompts.cs          # Interactive prompts
└── Installer/
    ├── InstallerEngine.cs      # Step pipeline orchestrator
    └── Steps/
        ├── IInstallerStep.cs   # Step interface
        ├── PreflightCheckStep.cs
        ├── VisualStudioStep.cs
        ├── ToolsSetupStep.cs
        ├── SkiaSetupStep.cs
        ├── SourceCloneStep.cs
        ├── BuildStep.cs
        ├── InstallStep.cs
        └── CleanupStep.cs
```

## License

This installer project is provided as-is. Aseprite itself is licensed under its own terms — see [Aseprite's EULA](https://github.com/aseprite/aseprite/blob/main/EULA.txt) for details.
