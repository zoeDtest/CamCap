@echo off
setlocal

set "SOURCE_DIR=%~dp0"
set "INSTALL_DIR=%LOCALAPPDATA%\CamCapture"
set "DESKTOP=%USERPROFILE%\Desktop"
set "STARTMENU=%APPDATA%\Microsoft\Windows\Start Menu\Programs"

echo Installing CamCapture to "%INSTALL_DIR%"...

if not exist "%INSTALL_DIR%" mkdir "%INSTALL_DIR%"
robocopy "%SOURCE_DIR%" "%INSTALL_DIR%" /E /R:1 /W:1 /NFL /NDL /NJH /NJS /NP /XF install.cmd >nul
if errorlevel 8 (
  echo File copy failed.
  exit /b 1
)

if not exist "%INSTALL_DIR%\captures" mkdir "%INSTALL_DIR%\captures"
if not exist "%INSTALL_DIR%\SdkLog" mkdir "%INSTALL_DIR%\SdkLog"

powershell -NoProfile -ExecutionPolicy Bypass -Command ^
  "$s=New-Object -ComObject WScript.Shell; " ^
  "$lnk=$s.CreateShortcut('%DESKTOP%\CamCapture.lnk'); " ^
  "$lnk.TargetPath='%INSTALL_DIR%\CamCapture.exe'; " ^
  "$lnk.WorkingDirectory='%INSTALL_DIR%'; " ^
  "$lnk.IconLocation='%INSTALL_DIR%\CamCapture.exe,0'; " ^
  "$lnk.Description='CamCapture'; " ^
  "$lnk.Save(); " ^
  "$lnk2=$s.CreateShortcut('%STARTMENU%\CamCapture.lnk'); " ^
  "$lnk2.TargetPath='%INSTALL_DIR%\CamCapture.exe'; " ^
  "$lnk2.WorkingDirectory='%INSTALL_DIR%'; " ^
  "$lnk2.IconLocation='%INSTALL_DIR%\CamCapture.exe,0'; " ^
  "$lnk2.Description='CamCapture'; " ^
  "$lnk2.Save(); " ^
  "$lnk3=$s.CreateShortcut('%STARTMENU%\CamCapture 卸载.lnk'); " ^
  "$lnk3.TargetPath='%INSTALL_DIR%\uninstall.cmd'; " ^
  "$lnk3.WorkingDirectory='%INSTALL_DIR%'; " ^
  "$lnk3.Description='卸载 CamCapture'; " ^
  "$lnk3.Save();"

echo Installation complete.
start "" "%INSTALL_DIR%\CamCapture.exe"
exit /b 0
