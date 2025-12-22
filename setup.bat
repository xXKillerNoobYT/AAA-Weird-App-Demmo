@echo off
REM WeirdToo Parts System - Setup and Installation
REM This script sets up the development environment for the first time

echo ========================================
echo WeirdToo Parts System - Setup
echo ========================================
echo.

REM Check for Python
echo [1/5] Checking Python installation...
where python >nul 2>nul
if %ERRORLEVEL% NEQ 0 (
    echo ERROR: Python not found in PATH
    echo Please install Python 3.12+ from https://www.python.org/downloads/
    pause
    exit /b 1
)

python --version
echo Python found!
echo.

REM Check for .NET SDK
echo [2/5] Checking .NET SDK installation...
where dotnet >nul 2>nul
if %ERRORLEVEL% NEQ 0 (
    echo WARNING: .NET SDK not found in PATH
    echo Please install .NET SDK 9.0 from https://aka.ms/dotnet-download
    echo.
    echo Continuing with Python-only setup...
    set DOTNET_AVAILABLE=0
) else (
    dotnet --version >nul 2>nul
    if %ERRORLEVEL% NEQ 0 (
        echo WARNING: .NET SDK not properly configured
        set DOTNET_AVAILABLE=0
    ) else (
        dotnet --version
        echo .NET SDK found!
        set DOTNET_AVAILABLE=1
    )
)
echo.

REM Create and activate Python virtual environment
echo [3/5] Setting up Python virtual environment...
if not exist ".venv" (
    echo Creating new virtual environment...
    python -m venv .venv
    if %ERRORLEVEL% NEQ 0 (
        echo ERROR: Failed to create virtual environment
        pause
        exit /b 1
    )
    echo Virtual environment created!
) else (
    echo Virtual environment already exists
)
echo.

REM Activate venv and install Python dependencies
echo [4/5] Installing Python dependencies...
call .venv\Scripts\activate.bat
if %ERRORLEVEL% NEQ 0 (
    echo ERROR: Failed to activate virtual environment
    pause
    exit /b 1
)

python -m pip install --upgrade pip
if exist "server\requirements.txt" (
    echo Installing server dependencies...
    python -m pip install -r server\requirements.txt
) else (
    echo No server\requirements.txt found - installing minimal deps...
    python -m pip install watchdog jsonschema
)
echo.

REM Build .NET projects if SDK is available
if %DOTNET_AVAILABLE%==1 (
    echo [5/5] Building .NET device projects...
    if exist "devices\DeviceClient\DeviceClient.csproj" (
        cd devices\DeviceClient
        dotnet restore
        dotnet build --configuration Release
        cd ..\..
        echo .NET projects built successfully!
    ) else (
        echo No .NET projects found yet - skipping build
    )
) else (
    echo [5/5] Skipping .NET build (SDK not available)
)
echo.

REM Create necessary directories
echo Creating directory structure...
if not exist "Cloud\Requests" mkdir Cloud\Requests
if not exist "Cloud\Responses" mkdir Cloud\Responses
if not exist "server" mkdir server
if not exist "devices" mkdir devices
if not exist "shared\schemas" mkdir shared\schemas
echo.

echo ========================================
echo Setup Complete!
echo ========================================
echo.
echo Next steps:
echo   1. To run the server: run-server.bat
echo   2. To run a device app: run-device.bat
echo.
if %DOTNET_AVAILABLE%==0 (
    echo NOTE: .NET SDK was not found. Install it to enable device apps:
    echo   https://aka.ms/dotnet-download
    echo.
)

pause
