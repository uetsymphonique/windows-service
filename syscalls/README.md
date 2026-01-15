# NtServiceInstaller - Technical Overview

## Technique Summary

Service installation using direct NT syscalls (`ntdll.dll`) for registry manipulation, bypassing user-mode API hooks on `advapi32.dll`. This advanced technique provides maximum stealth by only creating registry entries without triggering standard service creation APIs. Service becomes active after system reboot.

**Key Characteristics:**
- Direct `ntdll.dll` syscalls for registry operations
- Bypasses `advapi32.dll` CreateService/DeleteService APIs
- No service-specific event logs during install/uninstall
- Requires system reboot for SCM to load service
- Maximum EDR evasion

## MITRE ATT&CK Mapping

**Tactic:** Persistence, Privilege Escalation, Defense Evasion

**Techniques:**
- **T1543.003** - Create or Modify System Process: Windows Service
  - Creates service via registry manipulation
  - Service configured for auto-start
  - Runs as SYSTEM account

- **T1112** - Modify Registry
  - Direct registry key creation via NtCreateKey
  - Registry values set via NtSetValueKey
  - Persistence through service registry entries

- **T1106** - Native API
  - Uses native NT APIs (ntdll.dll) instead of Windows APIs
  - Bypasses user-mode hooks on advapi32.dll

**Sub-techniques:**
- Registry-only service creation
- NT layer syscalls for stealth
- Delayed activation (post-reboot)

**Detection Opportunities:**
- Security Event ID 4657 (Registry value modified - if auditing enabled)
- Security Event ID 4660 (Object deleted - if auditing enabled)
- Sysmon Event ID 1 (Process creation)
- Sysmon Event ID 7 (Image loaded - ntdll.dll without advapi32.dll service APIs)
- Sysmon Event ID 12 (Registry object added/deleted)
- Sysmon Event ID 13 (Registry value set)
- **Missing:** System Event ID 7045/7040 (service install/delete events)

**Evasion Characteristics:**
- No Event ID 7045 (service installed)
- No Event ID 4697 (service installed)
- No CreateService API call
- Appears as registry operations only

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
NtOpenKey(\Registry\Machine\SYSTEM\CurrentControlSet\Services)
  ↓
NtCreateKey(ServiceName)
  ↓
NtSetValueKey("Type", REG_DWORD, 16)
NtSetValueKey("Start", REG_DWORD, 2)
NtSetValueKey("ErrorControl", REG_DWORD, 1)
NtSetValueKey("ImagePath", REG_SZ, exePath)
NtSetValueKey("DisplayName", REG_SZ, displayName)
NtSetValueKey("ObjectName", REG_SZ, "LocalSystem")
  ↓
NtClose()
```

**Event Logs:**
- **System Log**: None (registry-only operation)
- **Security Log** (Event ID 4657): Registry value modified (if auditing enabled)
- **Sysmon** (Event ID 1): Process creation - NtServiceInstaller.exe
- **Sysmon** (Event ID 12): Registry object added - Service key created
- **Sysmon** (Event ID 13): Registry value set - Multiple values (Type, Start, ImagePath, etc.)

**Log Sources:**
- `%SystemRoot%\System32\winevt\Logs\Security.evtx`
- `%SystemRoot%\System32\winevt\Logs\Microsoft-Windows-Sysmon%4Operational.evtx`

**Note:** Service will NOT appear in System log or SCM until system reboot.

---

### Start Service

```
User Command
  ↓
Program.cs → HandleStart()
  ↓
ServiceInstaller.StartService()
  ↓
ServiceController.Start() [.NET wrapper → advapi32.dll]
  ↓
OpenSCManager()
OpenService()
StartService()
CloseServiceHandle()
```

**Event Logs:**
- **System Log** (Event ID 7036): Service entered running state
- **Sysmon** (Event ID 1): Process creation - NtServiceInstaller.exe

**Log Sources:**
- `%SystemRoot%\System32\winevt\Logs\System.evtx`
- `%SystemRoot%\System32\winevt\Logs\Microsoft-Windows-Sysmon%4Operational.evtx`

**Note:** Only works after system reboot when SCM has loaded the service.

---

### Stop Service

```
User Command
  ↓
Program.cs → HandleStop()
  ↓
ServiceInstaller.StopService()
  ↓
ServiceController.Stop() [.NET wrapper → advapi32.dll]
  ↓
OpenSCManager()
OpenService()
ControlService(SERVICE_CONTROL_STOP)
CloseServiceHandle()
```

**Event Logs:**
- **System Log** (Event ID 7036): Service entered stopped state
- **Sysmon** (Event ID 1): Process creation - NtServiceInstaller.exe

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
ServiceController.Stop() [if running]
  ↓
NtOpenKey(\Registry\Machine\SYSTEM\CurrentControlSet\Services)
  ↓
NtOpenKey(ServiceName)
  ↓
NtDeleteKey()
  ↓
NtClose()
```

**Event Logs:**
- **Security Log** (Event ID 4660): Object deleted (if auditing enabled)
- **Sysmon** (Event ID 1): Process creation - NtServiceInstaller.exe
- **Sysmon** (Event ID 12): Registry object deleted - Service key removed

**Log Sources:**
- `%SystemRoot%\System32\winevt\Logs\Security.evtx`

**Note:** No System log event for service deletion (registry-only).

---

### Query Status

```
User Command
  ↓
Program.cs → HandleStatus()
  ↓
ServiceInstaller.GetServiceStatus()
  ↓
ServiceController (reads from SCM)
```

**Event Logs:**
- None (read-only operation)

**Note:** Returns error if service not yet loaded into SCM (pre-reboot).

---

## Syscall Summary

| Operation | ntdll.dll Syscalls | advapi32.dll (fallback) |
|-----------|-------------------|------------------------|
| Install | NtOpenKey, NtCreateKey, NtSetValueKey, NtClose | None |
| Start | None | OpenSCManager, OpenService, StartService |
| Stop | None | OpenSCManager, OpenService, ControlService |
| Uninstall | NtOpenKey, NtDeleteKey, NtClose | OpenService, ControlService (stop) |
| Status | None | ServiceController query |

## DLL Loading Events

**Sysmon Event ID 7 (Image Loaded):**

Process: `NtServiceInstaller.exe`

**DLLs Loaded:**
- `ntdll.dll` - NT syscalls (primary)
- `kernel32.dll` - Core Windows APIs
- `advapi32.dll` - Only for ServiceController operations (start/stop)
- `sechost.dll` - Security host library
- Additional .NET runtime DLLs (if framework-dependent build)

**Log Source:**
- `Microsoft-Windows-Sysmon%4Operational.evtx`

**Key Difference:**
- **Install/Uninstall**: Only `ntdll.dll` registry operations
- **Start/Stop**: Loads `advapi32.dll` (fallback to SCM)

**Detection:** 
- Install: `ntdll.dll` loads WITHOUT subsequent `advapi32.dll` service APIs
- Start/Stop: `advapi32.dll` loads for ServiceController

## Detection Points

**Process Creation:**
- `NtServiceInstaller.exe` process spawn

**API Monitoring:**
- `ntdll!NtOpenKey`
- `ntdll!NtCreateKey`
- `ntdll!NtSetValueKey`
- `ntdll!NtDeleteKey`
- `ntdll!NtClose`

**Bypassed APIs:**
- `advapi32!CreateService` - NOT called during install
- `advapi32!DeleteService` - NOT called during uninstall
- `advapi32!OpenSCManager` - NOT called during install/uninstall

**Registry Modifications:**
- `HKLM\SYSTEM\CurrentControlSet\Services\<ServiceName>`

**File System:**
- Service executable path (ImagePath value)

## Key Differences from advapi32

**Install Operation:**
- No Event ID 7045 (service install) in System log
- No Event ID 4697 (service install) in Security log (unless registry auditing)
- Only registry modification events (if auditing enabled)

**Uninstall Operation:**
- No Event ID 7040 (service deleted) in System log
- Only registry deletion events (if auditing enabled)

**Stealth Advantage:**
- Bypasses advapi32.dll user-mode hooks
- No service-specific event log entries
- Appears as registry operations only
