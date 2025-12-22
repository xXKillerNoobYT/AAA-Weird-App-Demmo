@echo off
REM WeirdToo Parts System - Run Server
REM This script runs the Python server that watches for cloud requests

echo ========================================
echo WeirdToo Parts System - Server
echo ========================================
echo.

REM Check if virtual environment exists
if not exist ".venv\Scripts\activate.bat" (
    echo ERROR: Virtual environment not found
    echo Please run setup.bat first
    pause
    exit /b 1
)

REM Activate virtual environment
echo Activating Python environment...
call .venv\Scripts\activate.bat
if %ERRORLEVEL% NEQ 0 (
    echo ERROR: Failed to activate virtual environment
    pause
    exit /b 1
)
echo.

REM Check if server script exists
if not exist "server\watcher.py" (
    echo WARNING: server\watcher.py not found
    echo Creating a basic server watcher...
    
    if not exist "server" mkdir server
    
    REM Create a minimal watcher script
    echo import time > server\watcher.py
    echo import os >> server\watcher.py
    echo from pathlib import Path >> server\watcher.py
    echo. >> server\watcher.py
    echo print("WeirdToo Server - File Watcher") >> server\watcher.py
    echo print("Watching: Cloud/Requests/") >> server\watcher.py
    echo print("Press Ctrl+C to stop...") >> server\watcher.py
    echo print() >> server\watcher.py
    echo. >> server\watcher.py
    echo # TODO: Implement full file watcher >> server\watcher.py
    echo # This is a placeholder until the real implementation is added >> server\watcher.py
    echo. >> server\watcher.py
    echo while True: >> server\watcher.py
    echo     try: >> server\watcher.py
    echo         time.sleep(2) >> server\watcher.py
    echo     except KeyboardInterrupt: >> server\watcher.py
    echo         print("\nServer stopped") >> server\watcher.py
    echo         break >> server\watcher.py
    
    echo Placeholder script created
    echo.
)

REM Run the server
echo Starting server...
echo Press Ctrl+C to stop
echo.
python server\watcher.py

pause
