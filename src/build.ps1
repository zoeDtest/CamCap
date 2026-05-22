$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path "$PSScriptRoot\..\.."
$env:DOTNET_CLI_HOME = Join-Path $repoRoot ".dotnet-home"
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = "1"
$env:DOTNET_CLI_TELEMETRY_OPTOUT = "1"
$env:NUGET_PACKAGES = Join-Path $repoRoot ".nuget\packages"
$env:APPDATA = Join-Path $repoRoot ".appdata"
$env:LOCALAPPDATA = Join-Path $repoRoot ".localappdata"

$dotnet = Join-Path $repoRoot ".dotnet\dotnet.exe"
& $dotnet restore "$PSScriptRoot\IoCameraCapture.csproj" --configfile "$repoRoot\NuGet.Config"
& $dotnet build "$PSScriptRoot\IoCameraCapture.csproj" -c Release --no-restore
