$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$payloadDir = Join-Path $PSScriptRoot "single-file-payload"
$payloadZip = Join-Path $PSScriptRoot "single-file-payload.zip"
$sourceFile = Join-Path $PSScriptRoot "SingleFileInstaller.cs"
$iconFile = Join-Path $repoRoot "Program\camcapture.ico"
$outputDir = Join-Path $repoRoot "Installer"
$outputExe = Join-Path $outputDir "CamCapture_Setup_v2.1.1.exe"
$csc = Join-Path $env:WINDIR "Microsoft.NET\Framework64\v4.0.30319\csc.exe"

if (-not (Test-Path $csc)) {
    throw "C# compiler not found: $csc"
}

if (Test-Path $payloadDir) {
    Remove-Item -LiteralPath $payloadDir -Recurse -Force
}
New-Item -ItemType Directory -Path $payloadDir | Out-Null
New-Item -ItemType Directory -Path $outputDir -Force | Out-Null

$payloadItems = @(
    @{ Source = "Program"; Destination = "Program" },
    @{ Source = "Dependencies"; Destination = "Dependencies" },
    @{ Source = "Launcher"; Destination = "Launcher" },
    @{ Source = "Docs"; Destination = "Docs" }
)

foreach ($item in $payloadItems) {
    $source = Join-Path $repoRoot $item.Source
    if (-not (Test-Path $source)) {
        throw "Payload item not found: $source"
    }

    $destination = Join-Path $payloadDir $item.Destination
    Copy-Item -LiteralPath $source -Destination $destination -Recurse -Force
}

$payloadConfigDir = Join-Path $payloadDir "Config"
New-Item -ItemType Directory -Path $payloadConfigDir -Force | Out-Null
Copy-Item -LiteralPath (Join-Path $repoRoot "Config\default-camera-config.json") -Destination (Join-Path $payloadConfigDir "default-camera-config.json") -Force

if (Test-Path $payloadZip) {
    Remove-Item -LiteralPath $payloadZip -Force
}
Add-Type -AssemblyName System.IO.Compression.FileSystem
[System.IO.Compression.ZipFile]::CreateFromDirectory(
    $payloadDir,
    $payloadZip,
    [System.IO.Compression.CompressionLevel]::Optimal,
    $false)

if (Test-Path $outputExe) {
    Remove-Item -LiteralPath $outputExe -Force
}

& $csc `
  /target:winexe `
  /optimize+ `
  /out:$outputExe `
  /win32icon:$iconFile `
  /reference:System.Windows.Forms.dll `
  /reference:System.Drawing.dll `
  /reference:System.IO.Compression.dll `
  /reference:System.IO.Compression.FileSystem.dll `
  /codepage:65001 `
  /resource:$payloadZip,CamCaptureInstaller.payload.zip `
  $sourceFile

if (-not (Test-Path $outputExe)) {
    throw "Single-file installer was not created: $outputExe"
}

Remove-Item -LiteralPath $payloadZip -Force
Remove-Item -LiteralPath $payloadDir -Recurse -Force

Get-Item $outputExe
