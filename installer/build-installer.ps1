$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$stagingDir = Join-Path $PSScriptRoot "staging"
$outputExe = Join-Path $repoRoot "CamCapture_Setup_v1.1.0.exe"
$sedPath = Join-Path $PSScriptRoot "camcapture-installer.sed"

if (Test-Path $stagingDir) {
    Remove-Item -LiteralPath $stagingDir -Recurse -Force
}

New-Item -ItemType Directory -Path $stagingDir | Out-Null

$payloadItems = @(
    "CamCapture.exe",
    "CamCapture.dll",
    "CamCapture.deps.json",
    "CamCapture.runtimeconfig.json",
    "default-camera-config.json",
    "README.txt",
    "camcapture.ico",
    "run.ps1",
    "native"
)

foreach ($item in $payloadItems) {
    $source = Join-Path $repoRoot $item
    if (-not (Test-Path $source)) {
        throw "Payload item not found: $source"
    }

    Copy-Item -LiteralPath $source -Destination $stagingDir -Recurse -Force
}

Copy-Item -LiteralPath (Join-Path $PSScriptRoot "install.cmd") -Destination $stagingDir
Copy-Item -LiteralPath (Join-Path $PSScriptRoot "uninstall.cmd") -Destination $stagingDir

$fileNames = Get-ChildItem -LiteralPath $stagingDir | Select-Object -ExpandProperty Name
$stringLines = @()
$sourceLines = @()
for ($i = 0; $i -lt $fileNames.Count; $i++) {
    $token = "FILE$i"
    $stringLines += "$token=""$($fileNames[$i])"""
    $sourceLines += "%$token%="
}

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
InstallPrompt=
DisplayLicense=
FinishMessage=CamCapture Setup Complete.
TargetName=$outputExe
FriendlyName=CamCapture Setup
AppLaunched=install.cmd
PostInstallCmd=<None>
AdminQuietInstCmd=
UserQuietInstCmd=
SourceFiles=SourceFiles
[Strings]
InstallPrompt=
DisplayLicense=
FinishMessage=CamCapture Setup Complete.
TargetName=$outputExe
FriendlyName=CamCapture Setup
AppLaunched=install.cmd
PostInstallCmd=<None>
$($stringLines -join "`r`n")
[SourceFiles]
SourceFiles0=$stagingDir\
[SourceFiles0]
$($sourceLines -join "`r`n")
"@

Set-Content -LiteralPath $sedPath -Value $sed -Encoding ASCII

$iexpress = Join-Path $env:WINDIR "System32\iexpress.exe"
if (-not (Test-Path $iexpress)) {
    throw "IExpress not found: $iexpress"
}

& $iexpress /N /Q /M $sedPath

if (-not (Test-Path $outputExe)) {
    throw "Installer was not created: $outputExe"
}

Get-Item $outputExe
