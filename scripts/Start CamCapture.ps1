$ErrorActionPreference = "Stop"

$root = Resolve-Path (Join-Path $PSScriptRoot "..")
$exe = Join-Path $root "Program\CamCapture.exe"

if (-not (Test-Path $exe)) {
    throw "未找到主程序：$exe"
}

& $exe
