@echo off
setlocal

echo ============================================
echo   LinkittyDo - Stopping Application
echo ============================================
echo.

:: Kill the backend API window
taskkill /fi "WINDOWTITLE eq LinkittyDo-API*" /f >nul 2>&1
if %errorlevel% equ 0 (
    echo Stopped backend API.
) else (
    echo Backend API was not running.
)

:: Kill the frontend dev server window
taskkill /fi "WINDOWTITLE eq LinkittyDo-Web*" /f >nul 2>&1
if %errorlevel% equ 0 (
    echo Stopped frontend dev server.
) else (
    echo Frontend dev server was not running.
)

:: Also kill any orphaned dotnet/node processes on the specific ports
for /f "tokens=5" %%a in ('netstat -aon ^| findstr ":5157.*LISTENING" 2^>nul') do (
    taskkill /pid %%a /f >nul 2>&1
    echo Killed process on port 5157 (PID: %%a^)
)

for /f "tokens=5" %%a in ('netstat -aon ^| findstr ":5173.*LISTENING" 2^>nul') do (
    taskkill /pid %%a /f >nul 2>&1
    echo Killed process on port 5173 (PID: %%a^)
)

echo.
echo All LinkittyDo processes stopped.
