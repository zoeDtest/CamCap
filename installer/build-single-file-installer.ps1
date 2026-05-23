$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$payloadDir = Join-Path $PSScriptRoot "single-file-payload"
$payloadZip = Join-Path $payloadDir "payload.zip"
$sourceFile = Join-Path $PSScriptRoot "SingleFileInstaller.cs"
$iconFile = Join-Path $repoRoot "camcapture.ico"
$outputExe = Join-Path $repoRoot "CamCapture_Setup_v1.1.0.exe"
$csc = Join-Path $env:WINDIR "Microsoft.NET\Framework64\v4.0.30319\csc.exe"

if (-not (Test-Path $csc)) {
    throw "C# compiler not found: $csc"
}

if (Test-Path $payloadDir) {
    Remove-Item -LiteralPath $payloadDir -Recurse -Force
}
New-Item -ItemType Directory -Path $payloadDir | Out-Null

$payloadItems = @(
    "CamCapture.exe",
    "CamCapture.dll",
    "CamCapture.deps.json",
    "CamCapture.runtimeconfig.json",
    "camcapture.ico",
    "default-camera-config.json",
    "README.txt",
    "run.ps1",
    "native"
)

foreach ($item in $payloadItems) {
    $source = Join-Path $repoRoot $item
    if (-not (Test-Path $source)) {
        throw "Payload item not found: $source"
    }

    Copy-Item -LiteralPath $source -Destination $payloadDir -Recurse -Force
}

Compress-Archive -LiteralPath (Get-ChildItem -LiteralPath $payloadDir -Force).FullName -DestinationPath $payloadZip -CompressionLevel Optimal

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

Get-Item $outputExe
