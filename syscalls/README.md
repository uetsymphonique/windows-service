# NtServiceInstaller - Technical Overview

Direct ntdll.dll syscalls for registry manipulation.

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
