$ErrorActionPreference = "Stop"

$payloadSource = "E:\CamCapture"
$payloadDir = "E:\20260522\test\IoCameraCapture\installer\single-file-payload"
$payloadZip = Join-Path $payloadDir "payload.zip"
$sourceFile = "E:\20260522\test\IoCameraCapture\installer\SingleFileInstaller.cs"
$iconFile = "E:\20260522\test\IoCameraCapture\assets\camcapture.ico"
$outputExe = "E:\CamCapture\CamCapture_Setup_v1.1.0.exe"
$csc = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"

if (-not (Test-Path $payloadSource)) {
    throw "Payload source not found: $payloadSource"
}

if (-not (Test-Path $csc)) {
    throw "C# compiler not found: $csc"
}

if (Test-Path $payloadDir) {
    Remove-Item -LiteralPath $payloadDir -Recurse -Force
}
New-Item -ItemType Directory -Path $payloadDir | Out-Null

$payloadItems = Get-ChildItem -LiteralPath $payloadSource -Force |
    Where-Object { $_.Name -notlike "CamCapture_Setup_*.exe" }

if (-not $payloadItems) {
    throw "No payload files found in: $payloadSource"
}

Compress-Archive -LiteralPath $payloadItems.FullName -DestinationPath $payloadZip -CompressionLevel Optimal

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
