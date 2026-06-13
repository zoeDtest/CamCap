$ErrorActionPreference = "Stop"

$sourceRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$releaseRoot = Resolve-Path (Join-Path $sourceRoot "..")
$project = Join-Path $sourceRoot "src\IoCameraCapture.csproj"
$buildRoot = Join-Path $releaseRoot ".release-build"
$buildOutput = Join-Path $buildRoot "bin"
$buildObject = Join-Path $buildRoot "obj\"

$programDir = Join-Path $releaseRoot "Program"
$dependencyDir = Join-Path $releaseRoot "Dependencies"
$runnerDir = Join-Path $releaseRoot "Launcher"
$installerDir = Join-Path $releaseRoot "Installer"
$configDir = Join-Path $releaseRoot "Config"
$dataDir = Join-Path $releaseRoot "Data"
$documentDir = Join-Path $releaseRoot "Docs"

foreach ($directory in @($programDir, $dependencyDir, $runnerDir, $installerDir, $configDir, $dataDir, $documentDir)) {
    New-Item -ItemType Directory -Path $directory -Force | Out-Null
}
foreach ($directory in @("processing", "storage", "Logs", "SdkLog")) {
    New-Item -ItemType Directory -Path (Join-Path $dataDir $directory) -Force | Out-Null
}

dotnet build $project -c Release --no-restore `
    -p:IntermediateOutputPath=$buildObject `
    -p:OutputPath=$buildOutput

foreach ($file in @("CamCapture.exe", "CamCapture.dll", "CamCapture.deps.json", "CamCapture.runtimeconfig.json")) {
    Copy-Item -LiteralPath (Join-Path $buildOutput $file) -Destination (Join-Path $programDir $file) -Force
}
$legacyProgramConfig = Join-Path $programDir "default-camera-config.json"
if (Test-Path $legacyProgramConfig) {
    Remove-Item -LiteralPath $legacyProgramConfig -Force
}
Copy-Item -LiteralPath (Join-Path $sourceRoot "config\default-camera-config.json") -Destination (Join-Path $configDir "default-camera-config.json") -Force
Copy-Item -LiteralPath (Join-Path $sourceRoot "src\assets\camcapture.ico") -Destination (Join-Path $programDir "camcapture.ico") -Force
$mediaDir = Join-Path $programDir "Media"
New-Item -ItemType Directory -Path $mediaDir -Force | Out-Null
Copy-Item -LiteralPath (Join-Path $sourceRoot "src\assets\Media\ResultOK.wav") -Destination (Join-Path $mediaDir "ResultOK.wav") -Force
Copy-Item -LiteralPath (Join-Path $sourceRoot "src\assets\Media\ResultNG.wav") -Destination (Join-Path $mediaDir "ResultNG.wav") -Force
Copy-Item -LiteralPath (Join-Path $sourceRoot "scripts\Start CamCapture.ps1") -Destination (Join-Path $runnerDir "Start CamCapture.ps1") -Force
Copy-Item -LiteralPath (Join-Path $sourceRoot "docs\EngineeringGuide.md") -Destination (Join-Path $documentDir "EngineeringGuide.md") -Force
Copy-Item -LiteralPath (Join-Path $sourceRoot "docs\UserGuide.md") -Destination (Join-Path $documentDir "UserGuide.md") -Force

$legacyNative = Join-Path $releaseRoot "native"
$organizedNative = Join-Path $dependencyDir "native"
if (-not (Test-Path $organizedNative) -and (Test-Path $legacyNative)) {
    Copy-Item -LiteralPath $legacyNative -Destination $organizedNative -Recurse -Force
}
if (-not (Test-Path $organizedNative)) {
    throw "未找到 SDK 依赖目录：$organizedNative"
}

& (Join-Path $sourceRoot "installer\build-single-file-installer.ps1")

Get-ChildItem $programDir, $configDir, $runnerDir, $installerDir, $documentDir |
    Select-Object FullName, Length, LastWriteTime

if (Test-Path $buildRoot) {
    $resolvedBuildRoot = [System.IO.Path]::GetFullPath($buildRoot)
    if ($resolvedBuildRoot.StartsWith($releaseRoot.Path, [System.StringComparison]::OrdinalIgnoreCase)) {
        Remove-Item -LiteralPath $resolvedBuildRoot -Recurse -Force
    }
}
