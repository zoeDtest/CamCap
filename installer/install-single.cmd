@echo off
setlocal

set "DEFAULT_DIR=%LOCALAPPDATA%\CamCapture"
set "INSTALL_DIR="

for /f "usebackq delims=" %%I in (`powershell -NoProfile -ExecutionPolicy Bypass -Command "Add-Type -AssemblyName System.Windows.Forms; $dlg=New-Object System.Windows.Forms.FolderBrowserDialog; $dlg.Description='选择 CamCapture 安装目录'; $dlg.SelectedPath='%DEFAULT_DIR%'; if($dlg.ShowDialog() -eq [System.Windows.Forms.DialogResult]::OK){ [Console]::Write($dlg.SelectedPath) }"`) do set "INSTALL_DIR=%%I"

if not defined INSTALL_DIR (
  exit /b 1
)

echo Installing CamCapture to "%INSTALL_DIR%"...

if not exist "%INSTALL_DIR%" mkdir "%INSTALL_DIR%"

powershell -NoProfile -ExecutionPolicy Bypass -Command "Expand-Archive -LiteralPath '%~dp0payload.zip' -DestinationPath '%INSTALL_DIR%' -Force"
if errorlevel 1 (
  echo Extraction failed.
  exit /b 1
)

if not exist "%INSTALL_DIR%\captures" mkdir "%INSTALL_DIR%\captures"
if not exist "%INSTALL_DIR%\SdkLog" mkdir "%INSTALL_DIR%\SdkLog"

powershell -NoProfile -ExecutionPolicy Bypass -Command ^
  "$desktop=[Environment]::GetFolderPath('Desktop'); " ^
  "$start=[Environment]::GetFolderPath('Programs'); " ^
  "$s=New-Object -ComObject WScript.Shell; " ^
  "$lnk=$s.CreateShortcut((Join-Path $desktop 'CamCapture.lnk')); " ^
  "$lnk.TargetPath=(Join-Path '%INSTALL_DIR%' 'CamCapture.exe'); " ^
  "$lnk.WorkingDirectory='%INSTALL_DIR%'; " ^
  "$lnk.IconLocation=(Join-Path '%INSTALL_DIR%' 'CamCapture.exe') + ',0'; " ^
  "$lnk.Description='CamCapture'; " ^
  "$lnk.Save(); " ^
  "$lnk2=$s.CreateShortcut((Join-Path $start 'CamCapture.lnk')); " ^
  "$lnk2.TargetPath=(Join-Path '%INSTALL_DIR%' 'CamCapture.exe'); " ^
  "$lnk2.WorkingDirectory='%INSTALL_DIR%'; " ^
  "$lnk2.IconLocation=(Join-Path '%INSTALL_DIR%' 'CamCapture.exe') + ',0'; " ^
  "$lnk2.Description='CamCapture'; " ^
  "$lnk2.Save();"

@echo off> "%INSTALL_DIR%\uninstall.cmd"
echo setlocal>> "%INSTALL_DIR%\uninstall.cmd"
echo del "%USERPROFILE%\Desktop\CamCapture.lnk" ^>nul 2^>nul>> "%INSTALL_DIR%\uninstall.cmd"
echo del "%APPDATA%\Microsoft\Windows\Start Menu\Programs\CamCapture.lnk" ^>nul 2^>nul>> "%INSTALL_DIR%\uninstall.cmd"
echo cd /d "%%TEMP%%">> "%INSTALL_DIR%\uninstall.cmd"
echo timeout /t 1 /nobreak ^>nul>> "%INSTALL_DIR%\uninstall.cmd"
echo rd /s /q "%INSTALL_DIR%">> "%INSTALL_DIR%\uninstall.cmd"

echo Installation complete.
start "" "%INSTALL_DIR%\CamCapture.exe"
exit /b 0
