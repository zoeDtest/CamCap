# CamCapture 工程师代码结构

## 技术栈

- .NET 8 WinForms
- Windows x64
- 海康威视 HCNetSDK
- PowerShell 构建与打包脚本
- .NET 8 Windows x64 自包含发布

## 源码仓库

```text
github-upload/
├─ config/
│  └─ default-camera-config.json 初始配置模板
├─ src/                         主程序源码
│  ├─ Program.cs               界面、配置、抓图服务、日志与图片保留逻辑
│  ├─ HikvisionSdk.cs          HCNetSDK P/Invoke 定义
│  ├─ IoCameraCapture.csproj   主程序项目与版本号
│  └─ assets/
├─ installer/                   安装器源码与打包脚本
├─ scripts/
│  └─ 启动 CamCapture.ps1       发布目录启动脚本模板
├─ docs/
│  ├─ 工程师代码结构.md
│  ├─ 用户操作说明.md
│  └─ Git工作流.md
└─ README.md                    项目文档入口
```

## 发布目录

```text
E:\CamCapture/
├─ Program/                     主程序、图标与音频
├─ Dependencies/native/         HCNetSDK 与运行库
├─ Launcher/                    启动脚本
├─ Installer/                   当前版本安装包
├─ Config/
│  ├─ default-camera-config.json
│  └─ startup-config.json       用户保存的初始配置
├─ Data/
│  ├─ processing/
│  ├─ storage/
│  ├─ Logs/
│  ├─ SdkLog/
│  └─ startup.log
├─ Docs/                        用户与工程说明
└─ github-upload/               源码仓库
```

## 路径约定

`AppPaths` 统一管理运行路径：

- SDK：`Dependencies/native`
- 初始配置：`Config`
- 抓图处理：`Data/processing`
- 时间清理存储：`Data/storage`
- 程序日志：`Data/Logs`
- SDK 日志：`Data/SdkLog`
- 启动日志：`Data/startup.log`

`ImageRetentionManager` 支持两套独立策略：在存储文件夹内按图片数量删除最旧图片，以及在同一存储文件夹内按保留天数删除过期图片。两种策略均不会清理处理文件夹。

开发构建目录不叫 `Program` 时，程序会将当前可执行文件目录视为根目录，便于直接调试。

## 构建验证

```powershell
cd E:\CamCapture\github-upload
dotnet build src\IoCameraCapture.csproj -c Release
```

项目版本在主程序项目、安装器项目、安装器源码和打包脚本中保持一致。正式构建使用 `dotnet publish --self-contained true`，将 .NET 8 运行支撑一并放入 `Program` 并打入安装包。

## 打包

```powershell
cd E:\CamCapture\github-upload
.\installer\build-single-file-installer.ps1
```

脚本将 `Program`、`Config`、`Dependencies`、`Launcher` 和 `Docs` 打入单文件安装器，输出到：

```text
E:\CamCapture\Installer\CamCapture_Setup_v2.0.1.exe
```

## 提交范围

应提交源码、脚本、配置模板和说明文档。不要提交 `bin`、`obj`、运行日志、抓图、SDK 二进制和生成的安装包。
