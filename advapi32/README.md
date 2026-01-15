# WindowsServiceInstaller - Technical Overview

## Technique Summary

Service installation using direct Windows API calls (`advapi32.dll`) to create and manage Windows services without spawning `sc.exe`. This technique provides moderate stealth by avoiding command-line utilities while maintaining full service management capabilities with immediate SCM integration.

**Key Characteristics:**
- Direct `advapi32.dll` API calls via P/Invoke
- No child process spawning (no sc.exe)
- Service immediately available in SCM
- Standard Windows event logging
- Moderate EDR evasion

## MITRE ATT&CK Mapping

**Tactic:** Persistence, Privilege Escalation

**Techniques:**
- **T1543.003** - Create or Modify System Process: Windows Service
  - Uses CreateService API to install service
  - Service configured for auto-start
  - Runs as SYSTEM account

**Sub-techniques:**
- Service installation via advapi32.dll APIs
- Registry modification at `HKLM\SYSTEM\CurrentControlSet\Services`

**Detection Opportunities:**
- System Event ID 7045 (New service installed)
- Security Event ID 4697 (Service installed)
- Sysmon Event ID 1 (Process creation)
- Sysmon Event ID 7 (Image loaded - advapi32.dll)
- Sysmon Event ID 12/13 (Registry modifications)

---


## Code Flow

### Install Service

```
User Command
  ↓
Program.cs → HandleInstall()
  ↓
ServiceInstaller.InstallService()
  ↓
OpenSCManager(SC_MANAGER_CREATE_SERVICE)
  ↓
CreateService(SERVICE_WIN32_OWN_PROCESS, SERVICE_AUTO_START)
  ↓
CloseServiceHandle()
```

**Event Logs:**
- **System Log** (Event ID 7045): New service installed
- **Security Log** (Event ID 4697): Service installed (if auditing enabled)
- **Sysmon** (Event ID 1): Process creation - WindowsServiceInstaller.exe
- **Sysmon** (Event ID 13): Registry value set - Service registry keys
- **Sysmon** (Event ID 12): Registry object added - Service key created

**Log Sources:**
- `%SystemRoot%\System32\winevt\Logs\System.evtx`
- `%SystemRoot%\System32\winevt\Logs\Security.evtx`
- `%SystemRoot%\System32\winevt\Logs\Microsoft-Windows-Sysmon%4Operational.evtx`

---

### Start Service

```
User Command
  ↓
Program.cs → HandleStart()
  ↓
ServiceInstaller.StartService()
  ↓
OpenSCManager(SC_MANAGER_CONNECT)
  ↓
OpenService(SERVICE_START)
  ↓
StartService()
  ↓
CloseServiceHandle()
```

**Event Logs:**
- **System Log** (Event ID 7036): Service entered running state
- **Sysmon** (Event ID 1): Process creation - WindowsServiceInstaller.exe

**Log Sources:**
- `%SystemRoot%\System32\winevt\Logs\System.evtx`
- `%SystemRoot%\System32\winevt\Logs\Microsoft-Windows-Sysmon%4Operational.evtx`

---

### Stop Service

```
User Command
  ↓
Program.cs → HandleStop()
  ↓
ServiceInstaller.StopService()
  ↓
OpenSCManager(SC_MANAGER_CONNECT)
  ↓
OpenService(SERVICE_STOP)
  ↓
ControlService(SERVICE_CONTROL_STOP)
  ↓
CloseServiceHandle()
```

**Event Logs:**
- **System Log** (Event ID 7036): Service entered stopped state
- **Sysmon** (Event ID 1): Process creation - WindowsServiceInstaller.exe

**Log Sources:**
- `%SystemRoot%\System32\winevt\Logs\System.evtx`
- `%SystemRoot%\System32\winevt\Logs\Microsoft-Windows-Sysmon%4Operational.evtx`

---

### Uninstall Service

```
User Command
  ↓
Program.cs → HandleUninstall()
  ↓
ServiceInstaller.UninstallService()
  ↓
OpenSCManager(SC_MANAGER_CONNECT)
  ↓
OpenService(DELETE)
  ↓
ControlService(SERVICE_CONTROL_STOP) [if running]
  ↓
DeleteService()
  ↓
CloseServiceHandle()
```

**Event Logs:**
- **System Log** (Event ID 7040): Service deleted
- **Sysmon** (Event ID 1): Process creation - WindowsServiceInstaller.exe
- **Sysmon** (Event ID 12): Registry object deleted - Service key removed
- **Sysmon** (Event ID 13): Registry values deleted

**Log Sources:**
- `%SystemRoot%\System32\winevt\Logs\System.evtx`

---

### Query Status

```
User Command
  ↓
Program.cs → HandleStatus()
  ↓
ServiceInstaller.GetServiceStatus()
  ↓
OpenSCManager(SC_MANAGER_CONNECT)
  ↓
OpenService(SERVICE_QUERY_STATUS)
  ↓
QueryServiceStatus()
  ↓
CloseServiceHandle()
```

**Event Logs:**
- None (read-only operation)

---

## API Calls Summary

| Operation | advapi32.dll APIs |
|-----------|-------------------|
| Install | OpenSCManager, CreateService, CloseServiceHandle |
| Start | OpenSCManager, OpenService, StartService, CloseServiceHandle |
| Stop | OpenSCManager, OpenService, ControlService, CloseServiceHandle |
| Uninstall | OpenSCManager, OpenService, ControlService, DeleteService, CloseServiceHandle |
| Status | OpenSCManager, OpenService, QueryServiceStatus, CloseServiceHandle |

## DLL Loading Events

**Sysmon Event ID 7 (Image Loaded):**

Process: `WindowsServiceInstaller.exe`

**DLLs Loaded:**
- `ntdll.dll` - NT layer
- `kernel32.dll` - Core Windows APIs
- `advapi32.dll` - Service management APIs
- `sechost.dll` - Security host library
- Additional .NET runtime DLLs (if framework-dependent build)

**Log Source:**
- `Microsoft-Windows-Sysmon%4Operational.evtx`

**Detection:** Look for `advapi32.dll` loads followed by service-related API calls.

## Detection Points

**Process Creation:**
- `WindowsServiceInstaller.exe` process spawn

**API Monitoring:**
- `advapi32!OpenSCManager`
- `advapi32!CreateService`
- `advapi32!StartService`
- `advapi32!ControlService`
- `advapi32!DeleteService`

**Registry Modifications:**
- `HKLM\SYSTEM\CurrentControlSet\Services\<ServiceName>`

**File System:**
- Service executable path access
