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
    echo WARNING: Device project not found at devices\DeviceClient\
    echo.
    echo The .NET device application hasn't been created yet.
    echo Please run setup.bat and wait for the project scaffolding.
    echo.
    pause
    exit /b 1
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
