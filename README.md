# CamCapture

CamCapture 是基于 .NET 8 WinForms 与海康威视 HCNetSDK 的 Windows 高速相机抓图工具。

当前版本：`v2.1.1`

`v2.1.1` 优化图片自动清理：删除前处理只读属性、限制失败日志频率，并同步删除空文件夹。

正式安装包采用 Windows x64 自包含发布，已携带 .NET 8 运行支撑，目标电脑无需预装 .NET。

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
