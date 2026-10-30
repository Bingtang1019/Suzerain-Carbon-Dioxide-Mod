@echo off
setlocal
title Suzerain Mod Installer
echo ==============================================
echo   Suzerain Mod Installer
echo   (Oikonomia / HorrifyingBombardment / bundle)
echo ==============================================
echo.
echo  Tip: you can also DRAG the game folder (or Suzerain.exe)
echo       onto this file to install into a custom location.
echo.
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0install.ps1" %*
if errorlevel 1 (
  echo.
  echo [ERROR] Installer exited with an error - read the messages above.
)
echo.
pause