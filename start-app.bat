@echo off
setlocal

echo ============================================
echo   LinkittyDo - Starting Application
echo ============================================
echo.

:: Resolve paths relative to this script's location
set "ROOT=%~dp0"
set "API_DIR=%ROOT%src\LinkittyDo.Api"
set "WEB_DIR=%ROOT%src\linkittydo-web"

:: Check backend project exists
if not exist "%API_DIR%\LinkittyDo.Api.csproj" (
    echo ERROR: Backend project not found at %API_DIR%
    pause
    exit /b 1
)

:: Check frontend project exists
if not exist "%WEB_DIR%\package.json" (
    echo ERROR: Frontend project not found at %WEB_DIR%
    pause
    exit /b 1
)

:: Install npm dependencies if needed
if not exist "%WEB_DIR%\node_modules" (
    echo Installing frontend dependencies...
    pushd "%WEB_DIR%"
    call npm install
    if errorlevel 1 (
        echo ERROR: npm install failed
        popd
        pause
        exit /b 1
    )
    popd
    echo.
)

:: Start backend API in a new minimized window
echo Starting backend API on http://localhost:5157 ...
start "LinkittyDo-API" /min cmd /c "cd /d "%API_DIR%" && dotnet run --launch-profile http"

:: Start frontend dev server in a new minimized window
echo Starting frontend dev server on http://localhost:5173 ...
start "LinkittyDo-Web" /min cmd /c "cd /d "%WEB_DIR%" && npm run dev"

:: Wait for servers to start
echo.
echo Waiting for servers to start...
timeout /t 5 /nobreak >nul

:: Open the app in the default browser
echo Opening browser...
start "" "http://localhost:5173"

echo.
echo ============================================
echo   LinkittyDo is running!
echo.
echo   Frontend: http://localhost:5173
echo   Backend:  http://localhost:5157
echo   Swagger:  http://localhost:5157/swagger
echo.
echo   To stop: run stop-app.bat
echo ============================================
