# CamCapture

CamCapture is a Windows camera capture application built with .NET and Hikvision SDK bindings.

## Structure

- `src/` - application source code and runtime configuration template.
- `installer/` - installer source, packaging scripts, and shortcut/uninstall helpers.
- `installer/CamCaptureInstaller/` - .NET Windows Forms installer project.
- `docs/git-workflow.md` - personal Git branch and release workflow.

## Installer Behavior

The single-file installer lets the user choose an install location, creates or uses a `CamCapture` folder under that location, and asks whether to create a desktop shortcut.

The latest locally generated installer is:

```text
E:\CamCapture\CamCapture_Setup_v1.3.0.exe
```

## Git Workflow

Keep `main` stable. Use `develop` for daily integration and `feature/<name>` for incremental work.

Published versions should be tagged, for example `v1.3.0`, and may also have a matching `release/1.3.0` branch.

See `docs/git-workflow.md` for the full personal workflow.
