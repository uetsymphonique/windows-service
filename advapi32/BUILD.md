# WindowsServiceInstaller Build Guide

Direct advapi32.dll API calls (no sc.exe).

## Prerequisites

- .NET 8.0 SDK
- Windows OS
- Administrator privileges

## Build Commands

### Quick Build
```powershell
dotnet build -c Release
```
Output: `bin\Release\net8.0\WindowsServiceInstaller.exe` (requires .NET 8 runtime)

### Standalone Build (Recommended)
```powershell
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```
Output: `bin\Release\net8.0\win-x64\publish\WindowsServiceInstaller.exe` (~65 MB)

### Optimized Standalone
```powershell
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=true -p:DebugType=none
```
Output: `bin\Release\net8.0\win-x64\publish\WindowsServiceInstaller.exe` (~35 MB)

## Usage

```powershell
# Install service
.\WindowsServiceInstaller.exe install "C:\path\service.exe" ServiceName "Display Name" "Description"

# Start service
.\WindowsServiceInstaller.exe start ServiceName

# Check status
.\WindowsServiceInstaller.exe status ServiceName

# Stop service
.\WindowsServiceInstaller.exe stop ServiceName

# Uninstall service
.\WindowsServiceInstaller.exe uninstall ServiceName
```

## Notes

- Service starts immediately after installation
- No sc.exe process spawning
- Direct Windows API calls
- Run as Administrator
