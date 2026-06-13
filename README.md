# CamCapture

CamCapture 是基于 .NET 8 WinForms 与海康威视 HCNetSDK 的 Windows 高速相机抓图工具。

当前版本：`v2.0.0`

`v2.0.0` 在原有高速相机抓图功能之外，新增 VisionMarker TCP 结果监听页面，支持 OK/NG 结果解析、自动重连和音频提示。

详细文档统一存放在 [`docs`](docs/) 文件夹：

- [工程师代码结构](docs/EngineeringGuide.md)：源码目录、模块职责、构建与发布流程。
- [用户操作说明](docs/UserGuide.md)：安装、启动、相机配置、抓图与故障排查。
- [Git 工作流](docs/GitWorkflow.md)：分支、提交和版本发布约定。

发布目录采用以下结构：

```text
CamCapture/
├─ Program/
├─ Dependencies/
├─ Launcher/
├─ Installer/
├─ Config/
├─ Data/
└─ Docs/
```
