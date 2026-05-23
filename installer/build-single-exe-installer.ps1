$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$stagingDir = Join-Path $PSScriptRoot "single-staging"
$payloadDir = Join-Path $stagingDir "payload"
$payloadZip = Join-Path $stagingDir "payload.zip"
$installCmd = Join-Path $PSScriptRoot "install-single.cmd"
$sedPath = Join-Path $PSScriptRoot "camcapture-single-installer.sed"
$outputExe = Join-Path $repoRoot "CamCapture_Setup_v1.1.0.exe"

if (Test-Path $stagingDir) {
    Remove-Item -LiteralPath $stagingDir -Recurse -Force
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

$iexpress = Join-Path $env:WINDIR "System32\iexpress.exe"
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
