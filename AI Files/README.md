# AI Files Quick-Run

This folder contains execution artifacts for Smart Execute.

## Pre-flight checks (Windows/PowerShell)

```powershell
Set-Location "C:\Users\weird\AAA Weird App Demmo"
py -3.11 --version
py -3.11 -m venv .venv
.\.venv\Scripts\Activate.ps1
python -m pip install --upgrade pip

dotnet --info
```


## Notes

- Prefer built-in venv/pip and dotnet; avoid extra third-party tools unless approved.
- Communication model is file-based via cloud sync; no direct HTTP/gRPC.
- Server is the only writer to the authoritative DB.
