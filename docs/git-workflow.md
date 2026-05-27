# Git 工作流

本仓库采用适合个人开发者的简洁分支管理方式，目标是保持 `main` 稳定、提交历史清晰，并且可以随时回溯到发布版本。

## 分支说明

- `main`：稳定分支，只保存已经验证可运行的代码。
- `develop`：日常开发整合分支，用于保存已经完成本地验证的改动。
- `feature/<name>`：短期功能分支，用于实验、新功能或多次小步提交。
- `release/<version>`：发布分支，用于冻结某个已经发布的版本。

## 日常开发流程

开始新工作前，先切到 `develop` 并拉取最新代码：

```powershell
git checkout develop
git pull origin develop
```

为当前任务创建功能分支：

```powershell
git checkout -b feature/<short-name>
```

在功能分支上可以多次小步提交：

```powershell
git add <files>
git commit -m "wip: describe the step"
```

功能完成并测试通过后，合并回 `develop`。建议使用 `--squash` 把零散提交整理成一条清晰提交：

```powershell
git checkout develop
git merge --squash feature/<short-name>
git commit -m "feat: describe completed feature"
git branch -d feature/<short-name>
git push origin develop
```

## 发布流程

只从 `main` 发布稳定版本。

```powershell
git checkout main
git pull origin main
git merge --squash develop
git commit -m "Release <version>"
git tag v<version>
git push origin main
git push origin v<version>
git branch release/<version> v<version>
git push origin release/<version>
```

## 提交信息建议

推荐使用清晰的提交前缀：

- `feat:` 新功能
- `fix:` 修复问题
- `perf:` 性能优化
- `ui:` 界面和布局调整
- `docs:` 文档修改
- `build:` 构建、打包或安装脚本修改
- `release:` 版本发布

避免使用 `update`、`fix`、`final` 这类无法说明具体内容的提交信息。

## 不应提交到 Git 的文件

以下文件属于构建产物、运行数据或临时文件，不应提交到 Git：

- `bin/`、`obj/`
- `captures/`
- `Logs/`、`SdkLog/`、`*.log`
- 安装器临时 payload / staging 目录
- 生成的安装包 `.exe`

安装包建议作为本地交付文件保存，或上传到 GitHub Release，不要直接提交到源码历史中。
