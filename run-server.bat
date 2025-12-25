@echo off
REM WeirdToo Parts System - Run Server (Watcher + CloudWatcher API)
REM Starts Python file watcher AND ASP.NET Core API in one go

echo ========================================
echo WeirdToo Parts System - Server (Watcher + API)
echo ========================================
echo.

REM --- Python watcher bootstrap ---
if not exist ".venv\Scripts\activate.bat" (
    echo WARNING: Python virtual environment not found (.venv). Skipping watcher.
) else (
    if not exist "server\watcher.py" (
        echo Creating minimal watcher script...
        if not exist "server" mkdir server
        >server\watcher.py echo import time
        >>server\watcher.py echo from pathlib import Path
        >>server\watcher.py echo print("WeirdToo Server - File Watcher")
        >>server\watcher.py echo watch_dir=Path("Cloud/Requests"); watch_dir.mkdir(parents=True, exist_ok=True)
        >>server\watcher.py echo print(f"Watching: {watch_dir.as_posix()}")
        >>server\watcher.py echo print("Press Ctrl+C to stop...")
        >>server\watcher.py echo import sys
        >>server\watcher.py echo try:
        >>server\watcher.py echo ^    while True:
        >>server\watcher.py echo ^        print("heartbeat", end="\r", flush=True); time.sleep(2)
        >>server\watcher.py echo except KeyboardInterrupt:
        >>server\watcher.py echo ^    print("\nServer stopped")
    )
    echo Starting Python watcher in a new window...
    START "Watcher" cmd /c "call .venv\Scripts\activate.bat ^&^& python server\watcher.py"
)

REM --- .NET API (CloudWatcher) ---
where dotnet >nul 2>nul
if %ERRORLEVEL% NEQ 0 (
    echo ERROR: .NET SDK not found in PATH.
    echo If needed, run: powershell -ExecutionPolicy Bypass -File .\dotnet-install.ps1 -Channel 9.0
    pause
    exit /b 1
)

set CW_DIR=server\CloudWatcher
if not exist "%CW_DIR%\CloudWatcher.csproj" (
    echo ERROR: CloudWatcher project not found at %CW_DIR%.
    pause
    exit /b 1
)

echo Restoring and building CloudWatcher...
pushd "%CW_DIR%"
dotnet restore
if %ERRORLEVEL% NEQ 0 (
    echo ERROR: dotnet restore failed.
    popd
    pause
    exit /b 1
)

dotnet build --configuration Debug
if %ERRORLEVEL% NEQ 0 (
    echo ERROR: dotnet build failed.
    popd
    pause
    exit /b 1
)

echo Starting CloudWatcher API (Debug) in new window on http://localhost:5000 ...
START "CloudWatcher API" cmd /c "dotnet run --configuration Debug"
popd

echo.
echo All server components launched. This window can be closed.
exit /b 0
