# CamCapture

CamCapture 是一个 Windows 相机抓图工具，基于 .NET WinForms 和海康 SDK 绑定实现。

## 项目结构

- `src/`：主程序源码、默认配置和运行脚本。
- `installer/`：安装器源码、打包脚本、快捷方式和卸载辅助脚本。
- `installer/CamCaptureInstaller/`：WinForms 安装器项目。
- `docs/git-workflow.md`：个人开发分支管理和发布流程说明。

## 当前版本

当前稳定版本：`v1.3.0`

本地生成的最新安装包路径：

```text
E:\CamCapture\CamCapture_Setup_v1.3.0.exe
```

## 主要功能

- 多相机配置，最多支持 10 台相机。
- 每台相机独立登录、布防、触发和抓图。
- 支持配置保存和载入。
- 支持按相机、日期、小时分组存图。
- 支持内存抓图和后台异步写盘。
- 支持简洁 / 详细日志显示。
- 程序日志按天写入 `Logs/CamCapture_yyyyMMdd.log`。
- SDK 日志写入 `SdkLog/`。

## Git 分支约定

- `main`：稳定代码分支。
- `develop`：日常开发整合分支。
- `feature/<name>`：具体功能或实验分支。
- `release/<version>`：发布版本分支。

当前发布版本已打标签：`v1.3.0`。

详细流程见：[docs/git-workflow.md](docs/git-workflow.md)。
