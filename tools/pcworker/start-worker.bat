@echo off
title Confirmed Fire Poker - solve worker
cd /d "%~dp0"
powercfg /change standby-timeout-ac 0 >nul 2>&1
where node >nul 2>&1
if errorlevel 1 (
  echo.
  echo Node.js is not installed yet.
  echo Install the LTS version from https://nodejs.org ^(all defaults^), then double-click this file again.
  echo.
  pause
  exit /b
)
echo Leave this window open. This PC is solving at full speed.
echo.
node worker.js
pause
