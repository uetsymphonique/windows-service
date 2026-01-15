# NtServiceInstaller Build Guide

Direct ntdll.dll syscalls for maximum EDR evasion.

## Prerequisites

- .NET 8.0 SDK
- Windows OS
- Administrator privileges

## Build Commands

### Quick Build
```powershell
dotnet build -c Release
```
Output: `bin\Release\net8.0\NtServiceInstaller.exe` (requires .NET 8 runtime)

### Standalone Build (Recommended)
```powershell
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```
Output: `bin\Release\net8.0\win-x64\publish\NtServiceInstaller.exe` (~65 MB)

### Optimized Standalone
```powershell
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=true -p:DebugType=none
```
Output: `bin\Release\net8.0\win-x64\publish\NtServiceInstaller.exe` (~35 MB)

## Usage

```powershell
# Install service (creates registry keys only)
.\NtServiceInstaller.exe install "C:\path\service.exe" ServiceName "Display Name" "Description"

# Verify installation in registry
Get-ItemProperty "HKLM:\SYSTEM\CurrentControlSet\Services\ServiceName"

# Reboot system
Restart-Computer

# After reboot - Start service
.\NtServiceInstaller.exe start ServiceName

# Check status
.\NtServiceInstaller.exe status ServiceName

# Stop service
.\NtServiceInstaller.exe stop ServiceName

# Uninstall service
.\NtServiceInstaller.exe uninstall ServiceName
```

## Important Notes

- **Requires system reboot** - Service only exists in registry until reboot
- Bypasses advapi32.dll API hooks completely
- No CreateService/OpenSCManager calls
- Direct ntdll.dll registry manipulation
- Maximum stealth for EDR-heavy environments
- Run as Administrator

## Cleanup

```powershell
# Stop service
.\NtServiceInstaller.exe stop ServiceName

# Uninstall
.\NtServiceInstaller.exe uninstall ServiceName

# Manual registry cleanup if needed
Remove-Item "HKLM:\SYSTEM\CurrentControlSet\Services\ServiceName" -Recurse -Force
```
