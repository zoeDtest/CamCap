$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path "$PSScriptRoot\..\.."
$env:DOTNET_CLI_HOME = Join-Path $repoRoot ".dotnet-home"
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = "1"
$env:DOTNET_CLI_TELEMETRY_OPTOUT = "1"
$env:NUGET_PACKAGES = Join-Path $repoRoot ".nuget\packages"
$env:APPDATA = Join-Path $repoRoot ".appdata"
$env:LOCALAPPDATA = Join-Path $repoRoot ".localappdata"

$exe = Join-Path $PSScriptRoot "bin\Release\net8.0-windows\win-x64\CamCapture.exe"
if (-not (Test-Path $exe)) {
    & "$PSScriptRoot\build.ps1"
}

& $exe
