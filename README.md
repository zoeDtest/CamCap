# CamCapture

CamCapture 是一个 Windows 高速相机抓图工具，基于 .NET 8 WinForms 和海康威视 HCNetSDK 实现。程序用于相机登录、IO 报警触发、手动/模拟触发测试、JPEG 抓图、图片落盘和运行日志留存。

当前版本：`v1.4.2`

## 当前目录关系

本项目现在分为两个层次：

- `E:\CamCapture\github-upload`：源码仓库，保存可提交到 Git 的源码、文档、构建脚本和安装器代码。
- `E:\CamCapture`：本地发布/运行目录，保存已生成的可执行程序、安装包、SDK 运行库、抓图结果和日志。

构建脚本目前按这个目录关系工作：从 `github-upload\src` 读取源码，生成或打包时会使用上一级的 `E:\CamCapture` 作为发布根目录。

## 源码仓库结构

```text
github-upload/
├─ README.md
├─ .gitignore
├─ docs/
│  └─ git-workflow.md
├─ installer/
│  ├─ build-installer.ps1
│  ├─ build-single-exe-installer.ps1
│  ├─ build-single-file-installer.ps1
│  ├─ SingleFileInstaller.cs
│  ├─ install.cmd
│  ├─ install-single.cmd
│  ├─ uninstall.cmd
│  ├─ camcapture-installer.sed
│  ├─ camcapture-single-installer.sed
│  └─ CamCaptureInstaller/
│     ├─ CamCaptureInstaller.csproj
│     ├─ Program.cs
│     └─ assets/
│        └─ camcapture.ico
└─ src/
   ├─ IoCameraCapture.csproj
   ├─ Program.cs
   ├─ HikvisionSdk.cs
   ├─ default-camera-config.json
   ├─ build.ps1
   ├─ run.ps1
   ├─ README.md
   └─ assets/
      ├─ camcapture.ico
      └─ camcapture.png
```

## 发布目录结构

`E:\CamCapture` 是当前本机的交付目录，主要内容如下：

```text
E:\CamCapture/
├─ CamCapture.exe
├─ CamCapture.dll
├─ CamCapture.deps.json
├─ CamCapture.runtimeconfig.json
├─ CamCapture_Setup_v1.4.2.exe
├─ CamCapture_高速版_v1.4.2.exe
├─ default-camera-config.json
├─ README.txt
├─ run.ps1
├─ camcapture.ico
├─ native/
├─ captures/
├─ Logs/
├─ SdkLog/
└─ github-upload/
```

其中：

- `CamCapture.exe`：主程序。
- `CamCapture_Setup_v1.4.2.exe`：当前单文件安装器。
- `CamCapture_高速版_v1.4.2.exe`：当前高速版可执行文件。
- `native/`：海康威视 SDK 运行库及相关依赖。
- `captures/`：默认抓图输出目录。
- `Logs/`：程序日志目录，日志文件格式为 `CamCapture_yyyyMMdd.log`。
- `SdkLog/`：HCNetSDK 日志目录。
- `startup.log`：启动过程诊断日志。
- `default-camera-config.json`：默认相机配置模板。

## 主要功能

- 最多支持 10 台相机配置。
- 每台相机可独立设置设备 IP、端口、账号、密码、通道、通信编号、IO 型号和抓图目录。
- 支持相机登录、布防、撤防和状态显示。
- 支持真实 IO 报警触发、内部触发、外部触发模拟和手动连续触发。
- 支持按相机、日期、小时分组保存抓图。
- 支持为每台相机启用图片数量上限；每次写入成功后递归检查相机存图文件夹，超限时按最后写入时间删除最旧图片。
- 支持内存抓图和后台异步写盘，减少触发路径上的阻塞。
- 支持配置保存、配置载入和多相机模板复制。
- 支持简洁/详细两种界面日志模式。
- 程序日志和 SDK 日志会分别落盘，方便排查现场问题。

## 默认配置

默认配置文件位于：

```text
src/default-camera-config.json
```

发布目录中也会带一份：

```text
E:\CamCapture\default-camera-config.json
```

默认字段包括：

- 相机数量：`CameraCount`
- 设备连接：`Ip`、`Port`、`User`、`Password`
- 业务标识：`CommunicationNo`、`CameraFolder`
- 抓图参数：`Channel`、`PictureQuality`、`PictureSize`、`OutputRootDir`
- 存图保留：`LimitImageCount`、`MaxImageCount`
- 触发参数：`TriggerMode`、`ManualTriggerCount`、`ManualTriggerIntervalMs`
- IO 参数：`IoProfileKey`、`IoModel`、`AlarmInput`、`DebounceMs`、`PulseWidthMs`

首次运行建议先载入默认配置，再按现场相机和 IO 接线修改。

## 构建

源码项目位于 `github-upload\src`，目标框架为 `net8.0-windows`，运行时为 `win-x64`。

```powershell
cd E:\CamCapture\github-upload\src
.\build.ps1
```

注意：当前 `build.ps1` 使用 `E:\CamCapture` 下的本地 `.dotnet`、`.nuget`、`NuGet.Config` 等环境配置。如果迁移到其他机器，需要同步这些构建依赖，或者改成使用系统安装的 .NET SDK。

## 运行

开发目录运行：

```powershell
cd E:\CamCapture\github-upload\src
.\run.ps1
```

发布目录运行：

```powershell
cd E:\CamCapture
.\run.ps1
```

也可以直接双击：

```text
E:\CamCapture\CamCapture.exe
```

## 打包

当前推荐使用单文件安装器脚本：

```powershell
cd E:\CamCapture\github-upload\installer
.\build-single-file-installer.ps1
```

脚本会从 `E:\CamCapture` 收集以下发布内容：

- `CamCapture.exe`
- `CamCapture.dll`
- `CamCapture.deps.json`
- `CamCapture.runtimeconfig.json`
- `camcapture.ico`
- `default-camera-config.json`
- `README.txt`
- `run.ps1`
- `native/`

生成结果：

```text
E:\CamCapture\CamCapture_Setup_v1.4.2.exe
```

`installer/staging/`、`installer/single-staging/`、`installer/single-file-payload/` 都是临时打包目录，不应提交到 Git。

## Git 提交范围

应提交：

- `src/` 下的源码、项目文件、默认配置和资源。
- `installer/` 下的安装器源码、脚本和模板。
- `docs/` 下的开发文档。
- `README.md`、`.gitignore` 等仓库元数据。

不应提交：

- `bin/`、`obj/`
- `captures/`
- `Logs/`、`SdkLog/`、`startup.log`
- `installer/*payload*/` 和安装器 staging 目录
- 生成的安装包和发布可执行文件，例如 `CamCapture_Setup_*.exe`、`CamCapture_高速版_*.exe`

分支和发布流程见：

```text
docs/git-workflow.md
```

## 常见排查

- 启动失败：先看 `startup.log`，再看 `Logs/CamCapture_yyyyMMdd.log`。
- SDK 调用失败：检查 `native/` 是否完整，设备 IP、端口、账号密码是否正确，并查看 `SdkLog/`。
- 没有抓图输出：确认相机已登录并布防，`OutputRootDir` 可写，触发模式和 IO 输入号配置正确。
- 安装器缺文件：确认需要打包的发布文件已经存在于 `E:\CamCapture`，尤其是 `native/` 和 `README.txt`。
