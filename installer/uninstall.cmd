@echo off
setlocal

set "INSTALL_DIR=%~dp0"
set "DESKTOP=%USERPROFILE%\Desktop"
set "STARTMENU=%APPDATA%\Microsoft\Windows\Start Menu\Programs"

echo Removing shortcuts...
del "%DESKTOP%\CamCapture.lnk" >nul 2>nul
del "%STARTMENU%\CamCapture.lnk" >nul 2>nul
del "%STARTMENU%\CamCapture 卸载.lnk" >nul 2>nul

echo Removing installation folder...
cd /d "%TEMP%"
timeout /t 1 /nobreak >nul
rd /s /q "%INSTALL_DIR%"

echo CamCapture has been removed.
exit /b 0
