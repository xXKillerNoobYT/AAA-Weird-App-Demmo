# WeirdToo Parts System - Quick Start

## First Time Setup

Run the setup script to install dependencies and configure the environment:

```batch
setup.bat
```

This will:
- Check Python 3.12+ installation
- Check .NET SDK 9.0 installation (warns if missing)
- Create Python virtual environment
- Install Python dependencies (watchdog, jsonschema)
- Build .NET projects (if SDK available)
- Create required directories (Cloud/Requests, Cloud/Responses, etc.)

## Running the System

### Start the Server (required first)

```batch
run-server.bat
```

This runs the Python server that watches `Cloud/Requests/` for incoming JSON files and writes responses to `Cloud/Responses/`.

### Start a Device/User App

```batch
run-device.bat
```

This runs the .NET device application that can submit requests and poll for responses.

## System Architecture

- **Communication**: File-based only via `Cloud/` folders (no HTTP/REST/WebSocket)
- **Server**: Python, watches local folders synced by SharePoint/Google Drive
- **Devices**: .NET apps, write requests and poll responses
- **Database**: Server has PostgreSQL/SQL Server; devices have SQLite caches
- **Offline-first**: Devices work offline, sync when cloud available

## Prerequisites

- **Python 3.12+** (required)
- **.NET SDK 9.0** (required for device apps)
- **SharePoint or Google Drive** desktop sync (for cloud folder syncing)

## Directory Structure

```
WeirdToo Parts System/
├── setup.bat              # Initial setup
├── run-server.bat         # Start server
├── run-device.bat         # Start device app
├── server/                # Python server code
│   ├── watcher.py         # File watcher
│   └── requirements.txt   # Python dependencies
├── devices/               # .NET device apps
│   └── DeviceClient/      # Device client project
├── shared/                # Shared schemas
│   └── schemas/           # JSON schemas
└── Cloud/                 # Communication folder
    ├── Requests/          # Device → Server
    └── Responses/         # Server → Device
```

## Troubleshooting

**"Python not found"**: Install Python from https://www.python.org/downloads/

**".NET SDK not found"**: Install .NET SDK 9.0 from https://aka.ms/dotnet-download

**"Virtual environment failed"**: Ensure Python is in PATH and you have write permissions

**Server not seeing requests**: Check that Cloud/Requests/ folder exists and cloud sync is active
