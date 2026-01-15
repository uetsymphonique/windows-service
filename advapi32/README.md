# WindowsServiceInstaller - Technical Overview

Direct advapi32.dll API implementation for service management.

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
