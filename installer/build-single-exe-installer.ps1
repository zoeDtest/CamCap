$ErrorActionPreference = "Stop"

$payloadSource = "E:\CamCapture"
$stagingDir = "E:\20260522\test\IoCameraCapture\installer\single-staging"
$payloadZip = Join-Path $stagingDir "payload.zip"
$installCmd = "E:\20260522\test\IoCameraCapture\installer\install-single.cmd"
$sedPath = "E:\20260522\test\IoCameraCapture\installer\camcapture-single-installer.sed"
$outputExe = "E:\CamCapture_Setup_v1.1.0.exe"

if (-not (Test-Path $payloadSource)) {
    throw "Payload source not found: $payloadSource"
}

if (Test-Path $stagingDir) {
    Remove-Item -LiteralPath $stagingDir -Recurse -Force
}
New-Item -ItemType Directory -Path $stagingDir | Out-Null

Compress-Archive -Path (Join-Path $payloadSource "*") -DestinationPath $payloadZip -CompressionLevel Optimal
Copy-Item -LiteralPath $installCmd -Destination $stagingDir

$sed = @"
[Version]
Class=IEXPRESS
SEDVersion=3
[Options]
PackagePurpose=InstallApp
ShowInstallProgramWindow=1
HideExtractAnimation=0
UseLongFileName=1
InsideCompressed=1
CAB_FixedSize=0
CAB_ResvCodeSigning=0
RebootMode=N
InstallPrompt=%InstallPrompt%
DisplayLicense=%DisplayLicense%
FinishMessage=%FinishMessage%
TargetName=%TargetName%
FriendlyName=%FriendlyName%
AppLaunched=%AppLaunched%
PostInstallCmd=%PostInstallCmd%
AdminQuietInstCmd=
UserQuietInstCmd=
SourceFiles=SourceFiles
[Strings]
InstallPrompt=
DisplayLicense=
FinishMessage=CamCapture Setup Complete.
TargetName=$outputExe
FriendlyName=CamCapture Setup
AppLaunched=install-single.cmd
PostInstallCmd=<None>
FILE0=install-single.cmd
FILE1=payload.zip
[SourceFiles]
SourceFiles0=$stagingDir\
[SourceFiles0]
%FILE0%=
%FILE1%=
"@

Set-Content -LiteralPath $sedPath -Value $sed -Encoding ASCII

$iexpress = "$env:WINDIR\System32\iexpress.exe"
if (-not (Test-Path $iexpress)) {
    throw "IExpress not found: $iexpress"
}

if (Test-Path $outputExe) {
    Remove-Item -LiteralPath $outputExe -Force
}

& $iexpress /N /Q /M $sedPath

if (-not (Test-Path $outputExe)) {
    throw "Single-file installer was not created: $outputExe"
}

Get-Item $outputExe
