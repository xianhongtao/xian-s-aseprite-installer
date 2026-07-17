# xian's Aseprite 安装器

[🌐 English](README.md) | [🌐 中文](README.zh-CN.md)

> ⚠️ **免责声明**：本工具为**社区制作的非官方工具**，与 **Igara Studio S.A.** 及 Aseprite 团队**没有任何关联**。
> Aseprite 遵循其自身的 [EULA](https://github.com/aseprite/aseprite/blob/main/EULA.txt) 许可协议。使用本安装器即表示您同意遵守 Aseprite EULA 的条款。
> 💚 **如果您喜欢 Aseprite，建议[购买官方发售版本](https://aseprite.org)以支持开发者。**

一个用于 Windows 平台的 [Aseprite](https://github.com/aseprite/aseprite) **一键式、幂等、原子化**安装工具。

只需双击 `xiansAsepriteInstaller.exe`，它将自动：

1. **检测/安装** Visual Studio 2022 Build Tools（含 C++ 工作负载）
2. **下载** 便携版 CMake、Ninja 和 Git（不污染系统）
3. **下载** 符合 Aseprite 要求的预编译 Skia 库
4. **克隆** Aseprite 源码及所有子模块
5. **编译** 使用 CMake + Ninja + MSVC 从源码编译 Aseprite
6. **安装** 将编译完成的二进制文件原子化地安装到您指定的位置
7. **创建** 开始菜单快捷方式

## 快速开始

```powershell
# 构建安装器
.\build-installer.ps1

# 运行（双击 dist\xiansAsepriteInstaller.exe 或:）
.\dist\xiansAsepriteInstaller.exe
```

## 功能特性

- **单文件**：一个自包含的 `xiansAsepriteInstaller.exe`（约 10 MB），无需 .NET 运行时
- **幂等**：重复运行会自动跳过已完成步骤；可安全中断和恢复
- **原子化**：安装失败绝不会破坏现有的 Aseprite 安装
- **不污染系统**：构建工具下载到工作目录，而非安装到系统范围
- **用户或系统范围**：按用户安装（无需管理员权限）或系统范围安装（需要 UAC 提权）
- **TUI 界面**：交互式控制台界面，带进度条和彩色输出

## 命令行选项

```
xiansAsepriteInstaller.exe [[选项]]

  --system            安装到 C:\Program Files\Aseprite（需要管理员权限）
  --user              安装到 %LOCALAPPDATA%\Programs\Aseprite（默认）
  --auto-vs           自动安装 VS Build Tools（如果缺失，默认启用）
  --manual-vs         显示手动安装 VS 的说明
  --force             强制重新运行所有步骤（忽略已保存的状态）
  --no-shortcut       不创建开始菜单快捷方式
  --no-keep-build     安装后删除构建目录
  --version <ref>     构建指定的 Aseprite git 引用（默认：最新 main）
  --install-dir <p>   自定义安装目录
  --work-dir <p>      自定义工作目录
  --help, -h          显示帮助
```

## 工作原理

### 工作目录

所有下载文件、源码和构建产物均存储在：

```
%LOCALAPPDATA%\AsepriteInstaller\
├── tools/         # 便携版 CMake、Ninja、MinGit
├── deps/skia/     # 预编译 Skia 库
├── src/aseprite/  # Aseprite 源码（git clone）
├── build/         # CMake 构建输出
├── staging/       # 原子化安装暂存区
├── logs/          # 安装日志
└── state.json     # 幂等状态追踪
```

### 幂等性

每个步骤的完成状态都记录在 `state.json` 中。重新运行安装器时会读取该文件，并跳过已成功完成的步骤。使用 `--force` 可忽略状态，重新执行所有步骤。

### 原子化安装

安装前，编译好的二进制文件会被暂存到临时目录。只有在验证通过后，才会原子化地替换现有安装。如果替换失败，将自动从备份中恢复之前的版本。

### 依赖项

安装器会自动处理所有依赖：

| 依赖项 | 处理方式 |
|---|---|
| Visual Studio 2022 + C++ | 通过 `vswhere` 检测；自动安装（Build Tools）或手动安装 |
| CMake ≥ 3.20 | 下载便携版 ZIP 到工作目录 |
| Ninja | 下载便携版 ZIP 到工作目录 |
| Git | 下载 MinGit 便携版 ZIP 到工作目录 |
| Skia | 从 `aseprite/skia` GitHub 发布页面下载预编译 ZIP |
| Aseprite 源码 | 从 `aseprite/aseprite` 执行 `git clone --recursive` |
| 第三方库 | 从 Aseprite 的 git 子模块自动构建 |

## 构建安装器

### 前置要求

- .NET 10 SDK（或支持 Native AOT 的 .NET 8+）
- Visual Studio 2022（含 C++ 工作负载，用于安装器本身的 AOT 编译）

### 构建

```powershell
.\build-installer.ps1
```

输出：`dist/xiansAsepriteInstaller.exe`

## 项目结构

```
src/AsepriteInstaller/
├── AsepriteInstaller.csproj    # .NET Native AOT 项目配置
├── Program.cs                  # 入口点、UAC、参数解析
├── Models/
│   └── InstallOptions.cs       # 用户选项模型
├── State/
│   ├── InstallState.cs         # state.json 幂等状态追踪
│   └── InstallContext.cs       # 全局上下文（路径、工具、环境）
├── Utils/
│   ├── Logger.cs               # 控制台 + 文件双日志记录器
│   ├── Downloader.cs           # HTTP 下载（带进度和重试）
│   ├── ArchiveExtractor.cs     # ZIP 解压
│   ├── ProcessRunner.cs        # 外部进程执行
│   ├── VsDetector.cs           # 通过 vswhere 检测 VS 2022
│   ├── VsEnvironment.cs        # MSVC 环境捕获（vcvars64.bat）
│   └── PathUtils.cs            # 文件系统辅助方法
├── Tui/
│   ├── ConsoleApp.cs           # 横幅、计划、摘要面板
│   └── UserPrompts.cs          # 交互式提示
└── Installer/
    ├── InstallerEngine.cs      # 步骤管道编排器
    └── Steps/
        ├── IInstallerStep.cs   # 步骤接口
        ├── PreflightCheckStep.cs
        ├── VisualStudioStep.cs
        ├── ToolsSetupStep.cs
        ├── SkiaSetupStep.cs
        ├── SourceCloneStep.cs
        ├── BuildStep.cs
        ├── InstallStep.cs
        └── CleanupStep.cs
```

## 许可证

本安装器项目采用 **GNU General Public License v3.0 或更高版本** 授权——详情请参阅 [LICENSE](LICENSE) 文件。
Aseprite 本身遵循其自身许可条款——详情请参阅 [Aseprite 的 EULA](https://github.com/aseprite/aseprite/blob/main/EULA.txt)。
