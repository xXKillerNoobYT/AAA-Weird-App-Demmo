@echo off
REM WeirdToo Parts System - Run Device App
REM This script runs the .NET device/user application

echo ========================================
echo WeirdToo Parts System - Device App
echo ========================================
echo.

REM Check for .NET
where dotnet >nul 2>nul
if %ERRORLEVEL% NEQ 0 (
    echo ERROR: .NET SDK not found
    echo Please install .NET SDK 9.0 from https://aka.ms/dotnet-download
    pause
    exit /b 1
)

REM Check if device project exists
if not exist "devices\DeviceClient\DeviceClient.csproj" (
    echo WARNING: .NET device project not found at devices\DeviceClient\
    echo Falling back to Python device client if available...
    echo.
    
    REM Try Python virtual environment
    if not exist ".venv\Scripts\activate.bat" (
        echo ERROR: Python virtual environment not found (.venv)
        echo Please run setup.bat to create the environment.
        pause
        exit /b 1
    )

    REM Check for Python device script
    if not exist "device\python\send_request.py" (
        echo ERROR: Python device client script device\python\send_request.py not found
        echo Please add the device client or scaffold the .NET app.
        pause
        exit /b 1
    )

    echo Activating Python environment...
    call .venv\Scripts\activate.bat
    if %ERRORLEVEL% NEQ 0 (
        echo ERROR: Failed to activate Python virtual environment
        pause
        exit /b 1
    )

    echo.
    echo Starting Python device client...
    echo Press Ctrl+C to stop
    echo.
    python device\python\send_request.py

    set EXITCODE=%ERRORLEVEL%
    exit /b %EXITCODE%
)

REM Build and run the device app
echo Building device app...
cd devices\DeviceClient
dotnet build --configuration Release
if %ERRORLEVEL% NEQ 0 (
    echo ERROR: Build failed
    cd ..\..
    pause
    exit /b 1
)

echo.
echo Starting device app...
echo Press Ctrl+C to stop
echo.
dotnet run --configuration Release --no-build

cd ..\..
pause
