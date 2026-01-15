using System.ComponentModel;
using System.Runtime.InteropServices;

namespace WindowsServiceInstaller;

public class ServiceInstaller
{
    #region Windows API Constants
    private const uint SC_MANAGER_CREATE_SERVICE = 0x0002;
    private const uint SC_MANAGER_CONNECT = 0x0001;
    private const uint SERVICE_QUERY_STATUS = 0x0004;
    private const uint SERVICE_START = 0x0010;
    private const uint SERVICE_STOP = 0x0020;
    private const uint SERVICE_QUERY_CONFIG = 0x0001;
    private const uint SERVICE_CHANGE_CONFIG = 0x0002;
    private const uint DELETE = 0x00010000;
    
    private const uint SERVICE_WIN32_OWN_PROCESS = 0x00000010;
    private const uint SERVICE_AUTO_START = 0x00000002;
    private const uint SERVICE_DEMAND_START = 0x00000003;
    private const uint SERVICE_ERROR_NORMAL = 0x00000001;
    
    private const uint SERVICE_CONTROL_STOP = 0x00000001;
    #endregion

    #region Windows API Structures
    [StructLayout(LayoutKind.Sequential)]
    private struct SERVICE_STATUS
    {
        public uint dwServiceType;
        public uint dwCurrentState;
        public uint dwControlsAccepted;
        public uint dwWin32ExitCode;
        public uint dwServiceSpecificExitCode;
        public uint dwCheckPoint;
        public uint dwWaitHint;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct SERVICE_DESCRIPTION
    {
        public IntPtr lpDescription;
    }
    #endregion

    #region Windows API Imports
    [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr OpenSCManager(
        string? lpMachineName,
        string? lpDatabaseName,
        uint dwDesiredAccess);

    [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr CreateService(
        IntPtr hSCManager,
        string lpServiceName,
        string lpDisplayName,
        uint dwDesiredAccess,
        uint dwServiceType,
        uint dwStartType,
        uint dwErrorControl,
        string lpBinaryPathName,
        string? lpLoadOrderGroup,
        IntPtr lpdwTagId,
        string? lpDependencies,
        string? lpServiceStartName,
        string? lpPassword);

    [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr OpenService(
        IntPtr hSCManager,
        string lpServiceName,
        uint dwDesiredAccess);

    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern bool StartService(
        IntPtr hService,
        uint dwNumServiceArgs,
        string[]? lpServiceArgVectors);

    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern bool ControlService(
        IntPtr hService,
        uint dwControl,
        ref SERVICE_STATUS lpServiceStatus);

    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern bool DeleteService(IntPtr hService);

    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern bool CloseServiceHandle(IntPtr hSCObject);

    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern bool QueryServiceStatus(
        IntPtr hService,
        ref SERVICE_STATUS lpServiceStatus);

    [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool ChangeServiceConfig2(
        IntPtr hService,
        uint dwInfoLevel,
        ref SERVICE_DESCRIPTION lpInfo);

    private const uint SERVICE_CONFIG_DESCRIPTION = 1;
    #endregion

    public static bool InstallService(string exePath, string serviceName, string? displayName = null, string? description = null)
    {
        IntPtr scManager = IntPtr.Zero;
        IntPtr service = IntPtr.Zero;

        try
        {
            if (!File.Exists(exePath))
            {
                Console.WriteLine($"Error: Executable file not found: {exePath}");
                return false;
            }

            displayName ??= serviceName;

            // Open Service Control Manager
            scManager = OpenSCManager(null, null, SC_MANAGER_CREATE_SERVICE | SC_MANAGER_CONNECT);
            if (scManager == IntPtr.Zero)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            // Create the service
            service = CreateService(
                scManager,
                serviceName,
                displayName,
                SERVICE_QUERY_CONFIG | SERVICE_CHANGE_CONFIG,
                SERVICE_WIN32_OWN_PROCESS,
                SERVICE_AUTO_START,
                SERVICE_ERROR_NORMAL,
                exePath,
                null,
                IntPtr.Zero,
                null,
                null,
                null);

            if (service == IntPtr.Zero)
            {
                int error = Marshal.GetLastWin32Error();
                throw new Win32Exception(error);
            }

            Console.WriteLine($"Service '{serviceName}' created successfully.");

            // Set description if provided
            if (!string.IsNullOrEmpty(description))
            {
                IntPtr descPtr = Marshal.StringToHGlobalUni(description);
                try
                {
                    SERVICE_DESCRIPTION serviceDesc = new SERVICE_DESCRIPTION
                    {
                        lpDescription = descPtr
                    };

                    if (ChangeServiceConfig2(service, SERVICE_CONFIG_DESCRIPTION, ref serviceDesc))
                    {
                        Console.WriteLine("Service description set.");
                    }
                }
                finally
                {
                    Marshal.FreeHGlobal(descPtr);
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error installing service: {ex.Message}");
            return false;
        }
        finally
        {
            if (service != IntPtr.Zero)
                CloseServiceHandle(service);
            if (scManager != IntPtr.Zero)
                CloseServiceHandle(scManager);
        }
    }

    public static bool UninstallService(string serviceName)
    {
        IntPtr scManager = IntPtr.Zero;
        IntPtr service = IntPtr.Zero;

        try
        {
            // Stop service first if running
            if (IsServiceRunning(serviceName))
            {
                Console.WriteLine($"Stopping service '{serviceName}'...");
                StopService(serviceName);
                Thread.Sleep(2000);
            }

            // Open Service Control Manager
            scManager = OpenSCManager(null, null, SC_MANAGER_CONNECT);
            if (scManager == IntPtr.Zero)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            // Open the service
            service = OpenService(scManager, serviceName, DELETE);
            if (service == IntPtr.Zero)
            {
                int error = Marshal.GetLastWin32Error();
                throw new Win32Exception(error);
            }

            // Delete the service
            if (!DeleteService(service))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            Console.WriteLine($"Service '{serviceName}' uninstalled successfully.");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error uninstalling service: {ex.Message}");
            return false;
        }
        finally
        {
            if (service != IntPtr.Zero)
                CloseServiceHandle(service);
            if (scManager != IntPtr.Zero)
                CloseServiceHandle(scManager);
        }
    }

    public static bool StartService(string serviceName)
    {
        IntPtr scManager = IntPtr.Zero;
        IntPtr service = IntPtr.Zero;

        try
        {
            // Open Service Control Manager
            scManager = OpenSCManager(null, null, SC_MANAGER_CONNECT);
            if (scManager == IntPtr.Zero)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            // Open the service
            service = OpenService(scManager, serviceName, SERVICE_START | SERVICE_QUERY_STATUS);
            if (service == IntPtr.Zero)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            // Check if already running
            SERVICE_STATUS status = new SERVICE_STATUS();
            if (QueryServiceStatus(service, ref status))
            {
                if (status.dwCurrentState == 4) // SERVICE_RUNNING
                {
                    Console.WriteLine($"Service '{serviceName}' is already running.");
                    return true;
                }
            }

            // Start the service
            Console.WriteLine($"Starting service '{serviceName}'...");
            if (!StartService(service, 0, null))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            // Wait for service to start
            for (int i = 0; i < 30; i++)
            {
                Thread.Sleep(1000);
                if (QueryServiceStatus(service, ref status) && status.dwCurrentState == 4)
                {
                    Console.WriteLine($"Service '{serviceName}' started successfully.");
                    return true;
                }
            }

            Console.WriteLine($"Service '{serviceName}' start initiated but status unknown.");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error starting service: {ex.Message}");
            return false;
        }
        finally
        {
            if (service != IntPtr.Zero)
                CloseServiceHandle(service);
            if (scManager != IntPtr.Zero)
                CloseServiceHandle(scManager);
        }
    }

    public static bool StopService(string serviceName)
    {
        IntPtr scManager = IntPtr.Zero;
        IntPtr service = IntPtr.Zero;

        try
        {
            // Open Service Control Manager
            scManager = OpenSCManager(null, null, SC_MANAGER_CONNECT);
            if (scManager == IntPtr.Zero)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            // Open the service
            service = OpenService(scManager, serviceName, SERVICE_STOP | SERVICE_QUERY_STATUS);
            if (service == IntPtr.Zero)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            // Check if already stopped
            SERVICE_STATUS status = new SERVICE_STATUS();
            if (QueryServiceStatus(service, ref status))
            {
                if (status.dwCurrentState == 1) // SERVICE_STOPPED
                {
                    Console.WriteLine($"Service '{serviceName}' is already stopped.");
                    return true;
                }
            }

            // Stop the service
            Console.WriteLine($"Stopping service '{serviceName}'...");
            if (!ControlService(service, SERVICE_CONTROL_STOP, ref status))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            // Wait for service to stop
            for (int i = 0; i < 30; i++)
            {
                Thread.Sleep(1000);
                if (QueryServiceStatus(service, ref status) && status.dwCurrentState == 1)
                {
                    Console.WriteLine($"Service '{serviceName}' stopped successfully.");
                    return true;
                }
            }

            Console.WriteLine($"Service '{serviceName}' stop initiated but status unknown.");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error stopping service: {ex.Message}");
            return false;
        }
        finally
        {
            if (service != IntPtr.Zero)
                CloseServiceHandle(service);
            if (scManager != IntPtr.Zero)
                CloseServiceHandle(scManager);
        }
    }

    public static void GetServiceStatus(string serviceName)
    {
        IntPtr scManager = IntPtr.Zero;
        IntPtr service = IntPtr.Zero;

        try
        {
            // Open Service Control Manager
            scManager = OpenSCManager(null, null, SC_MANAGER_CONNECT);
            if (scManager == IntPtr.Zero)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            // Open the service
            service = OpenService(scManager, serviceName, SERVICE_QUERY_STATUS | SERVICE_QUERY_CONFIG);
            if (service == IntPtr.Zero)
            {
                Console.WriteLine($"Service '{serviceName}' does not exist.");
                return;
            }

            SERVICE_STATUS status = new SERVICE_STATUS();
            if (!QueryServiceStatus(service, ref status))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            Console.WriteLine($"Service Name: {serviceName}");
            Console.WriteLine($"Status: {GetStatusString(status.dwCurrentState)}");
            Console.WriteLine($"Service Type: {GetServiceTypeString(status.dwServiceType)}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting service status: {ex.Message}");
        }
        finally
        {
            if (service != IntPtr.Zero)
                CloseServiceHandle(service);
            if (scManager != IntPtr.Zero)
                CloseServiceHandle(scManager);
        }
    }

    private static bool IsServiceRunning(string serviceName)
    {
        IntPtr scManager = IntPtr.Zero;
        IntPtr service = IntPtr.Zero;

        try
        {
            scManager = OpenSCManager(null, null, SC_MANAGER_CONNECT);
            if (scManager == IntPtr.Zero)
                return false;

            service = OpenService(scManager, serviceName, SERVICE_QUERY_STATUS);
            if (service == IntPtr.Zero)
                return false;

            SERVICE_STATUS status = new SERVICE_STATUS();
            if (QueryServiceStatus(service, ref status))
            {
                return status.dwCurrentState == 4; // SERVICE_RUNNING
            }

            return false;
        }
        catch
        {
            return false;
        }
        finally
        {
            if (service != IntPtr.Zero)
                CloseServiceHandle(service);
            if (scManager != IntPtr.Zero)
                CloseServiceHandle(scManager);
        }
    }

    private static string GetStatusString(uint state)
    {
        return state switch
        {
            1 => "Stopped",
            2 => "Start Pending",
            3 => "Stop Pending",
            4 => "Running",
            5 => "Continue Pending",
            6 => "Pause Pending",
            7 => "Paused",
            _ => $"Unknown ({state})"
        };
    }

    private static string GetServiceTypeString(uint type)
    {
        return type switch
        {
            0x00000010 => "Win32 Own Process",
            0x00000020 => "Win32 Share Process",
            _ => $"Unknown ({type:X})"
        };
    }
}
