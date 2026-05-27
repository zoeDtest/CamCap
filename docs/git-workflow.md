# Git Workflow

This repository uses a simple personal-developer workflow.

## Branches

- `main`: stable, runnable code only. Keep this branch clean.
- `develop`: daily integration branch for tested local work before release.
- `feature/<name>`: short-lived work branches for experiments and incremental commits.
- `release/<version>`: frozen release branch for a published version.

## Daily Work

```powershell
git checkout develop
git pull origin develop
git checkout -b feature/<short-name>
```

Commit small steps on the feature branch:

```powershell
git add <files>
git commit -m "wip: describe the step"
```

When the feature is ready, squash it into `develop`:

```powershell
git checkout develop
git merge --squash feature/<short-name>
git commit -m "feat: describe completed feature"
git branch -d feature/<short-name>
git push origin develop
```

## Release

Only release stable builds from `main`.

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

## Commit Message Style

Use clear prefixes:

- `feat:` new feature
- `fix:` bug fix
- `perf:` performance improvement
- `ui:` layout or visual change
- `docs:` documentation change
- `build:` build or installer change
- `release:` version release

Avoid vague messages such as `update`, `fix`, or `final`.

## Files to Keep Out of Git

Do not commit generated files:

- `bin/`, `obj/`
- `captures/`
- `Logs/`, `SdkLog/`, `*.log`
- installer staging payloads
- generated setup `.exe` packages

Generated installers should be kept as local artifacts or attached to GitHub releases, not committed to source history.
