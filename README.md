# CamCapture

CamCapture is a Windows camera capture application built with .NET and Hikvision SDK bindings.

## Structure

- `src/` - application source code and runtime configuration template.
- `installer/` - installer source, packaging scripts, and shortcut/uninstall helpers.
- `installer/CamCaptureInstaller/` - .NET Windows Forms installer project.

## Installer Behavior

The single-file installer lets the user choose an install location, creates or uses a `CamCapture` folder under that location, and asks whether to create a desktop shortcut.

The latest locally generated installer is:

```text
E:\CamCapture\CamCapture_Setup_v1.1.0.exe
```
