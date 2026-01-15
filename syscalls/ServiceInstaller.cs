using System.Runtime.InteropServices;
using System.ServiceProcess;
using System.Text;

namespace NtServiceInstaller;

public static class ServiceInstaller
{
    public static bool InstallService(string exePath, string serviceName, string? displayName = null, string? description = null)
    {
        IntPtr servicesKeyHandle = IntPtr.Zero;
        IntPtr serviceKeyHandle = IntPtr.Zero;
        
        try
        {
            if (!File.Exists(exePath))
            {
                Console.WriteLine($"Error: Executable file not found: {exePath}");
                return false;
            }
            
            displayName ??= serviceName;
            
            // Open HKLM\SYSTEM\CurrentControlSet\Services
            var servicesPath = NtHelper.InitUnicodeString(NtConstants.SERVICES_KEY);
            var servicesOa = NtHelper.InitObjectAttributes(ref servicesPath);
            
            uint status = NtApi.NtOpenKey(
                out servicesKeyHandle,
                NtConstants.KEY_CREATE_SUB_KEY,
                ref servicesOa);
                
            NtHelper.FreeUnicodeString(ref servicesPath);
            
            if (!NtHelper.NT_SUCCESS(status))
            {
                Console.WriteLine($"Failed to open Services key: 0x{status:X8}");
                return false;
            }
            
            // Create service subkey
            var serviceKeyPath = NtHelper.InitUnicodeString(serviceName);
            var serviceOa = NtHelper.InitObjectAttributes(ref serviceKeyPath, rootDirectory: servicesKeyHandle);
            
            status = NtApi.NtCreateKey(
                out serviceKeyHandle,
                NtConstants.KEY_ALL_ACCESS,
                ref serviceOa,
                0,
                IntPtr.Zero,
                0,
                out uint disposition);
                
            NtHelper.FreeUnicodeString(ref serviceKeyPath);
            
            if (!NtHelper.NT_SUCCESS(status))
            {
                Console.WriteLine($"Failed to create service key: 0x{status:X8}");
                return false;
            }
            
            Console.WriteLine($"Service key created (disposition: {disposition})");
            
            // Set Type = SERVICE_WIN32_OWN_PROCESS
            if (!SetRegistryDWord(serviceKeyHandle, "Type", NtConstants.SERVICE_WIN32_OWN_PROCESS))
                return false;
            
            // Set Start = SERVICE_AUTO_START
            if (!SetRegistryDWord(serviceKeyHandle, "Start", NtConstants.SERVICE_AUTO_START))
                return false;
            
            // Set ErrorControl = SERVICE_ERROR_NORMAL
            if (!SetRegistryDWord(serviceKeyHandle, "ErrorControl", NtConstants.SERVICE_ERROR_NORMAL))
                return false;
            
            // Set ImagePath
            if (!SetRegistryString(serviceKeyHandle, "ImagePath", exePath))
                return false;
            
            // Set DisplayName
            if (!SetRegistryString(serviceKeyHandle, "DisplayName", displayName))
                return false;
            
            // Set ObjectName = LocalSystem
            if (!SetRegistryString(serviceKeyHandle, "ObjectName", "LocalSystem"))
                return false;
            
            // Set Description if provided
            if (!string.IsNullOrEmpty(description))
            {
                if (!SetRegistryString(serviceKeyHandle, "Description", description))
                    Console.WriteLine("Warning: Failed to set description");
            }
            
            Console.WriteLine($"Service '{serviceName}' installed successfully via NT syscalls");
            Console.WriteLine("Note: Service may require system restart or manual SCM refresh to appear");
            
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error installing service: {ex.Message}");
            return false;
        }
        finally
        {
            if (serviceKeyHandle != IntPtr.Zero)
                NtApi.NtClose(serviceKeyHandle);
            if (servicesKeyHandle != IntPtr.Zero)
                NtApi.NtClose(servicesKeyHandle);
        }
    }
    
    public static bool UninstallService(string serviceName)
    {
        IntPtr servicesKeyHandle = IntPtr.Zero;
        IntPtr serviceKeyHandle = IntPtr.Zero;
        
        try
        {
            // Stop service first if running
            try
            {
                using var sc = new ServiceController(serviceName);
                if (sc.Status != ServiceControllerStatus.Stopped)
                {
                    Console.WriteLine($"Stopping service '{serviceName}'...");
                    sc.Stop();
                    sc.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(10));
                }
            }
            catch
            {
                // Service may not exist in SCM yet
            }
            
            // Open Services key
            var servicesPath = NtHelper.InitUnicodeString(NtConstants.SERVICES_KEY);
            var servicesOa = NtHelper.InitObjectAttributes(ref servicesPath);
            
            uint status = NtApi.NtOpenKey(
                out servicesKeyHandle,
                NtConstants.KEY_ENUMERATE_SUB_KEYS,
                ref servicesOa);
                
            NtHelper.FreeUnicodeString(ref servicesPath);
            
            if (!NtHelper.NT_SUCCESS(status))
            {
                Console.WriteLine($"Failed to open Services key: 0x{status:X8}");
                return false;
            }
            
            // Open service subkey
            var serviceKeyPath = NtHelper.InitUnicodeString(serviceName);
            var serviceOa = NtHelper.InitObjectAttributes(ref serviceKeyPath, rootDirectory: servicesKeyHandle);
            
            status = NtApi.NtOpenKey(
                out serviceKeyHandle,
                NtConstants.DELETE,
                ref serviceOa);
                
            NtHelper.FreeUnicodeString(ref serviceKeyPath);
            
            if (!NtHelper.NT_SUCCESS(status))
            {
                if (status == NtConstants.STATUS_OBJECT_NAME_NOT_FOUND)
                {
                    Console.WriteLine($"Service '{serviceName}' does not exist");
                }
                else
                {
                    Console.WriteLine($"Failed to open service key: 0x{status:X8}");
                }
                return false;
            }
            
            // Delete the key
            status = NtApi.NtDeleteKey(serviceKeyHandle);
            
            if (!NtHelper.NT_SUCCESS(status))
            {
                Console.WriteLine($"Failed to delete service key: 0x{status:X8}");
                return false;
            }
            
            Console.WriteLine($"Service '{serviceName}' uninstalled successfully via NT syscalls");
            
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error uninstalling service: {ex.Message}");
            return false;
        }
        finally
        {
            if (serviceKeyHandle != IntPtr.Zero)
                NtApi.NtClose(serviceKeyHandle);
            if (servicesKeyHandle != IntPtr.Zero)
                NtApi.NtClose(servicesKeyHandle);
        }
    }
    
    public static bool StartService(string serviceName)
    {
        try
        {
            using var service = new ServiceController(serviceName);
            
            if (service.Status == ServiceControllerStatus.Running)
            {
                Console.WriteLine($"Service '{serviceName}' is already running");
                return true;
            }
            
            Console.WriteLine($"Starting service '{serviceName}'...");
            service.Start();
            service.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(30));
            
            Console.WriteLine($"Service '{serviceName}' started successfully");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error starting service: {ex.Message}");
            return false;
        }
    }
    
    public static bool StopService(string serviceName)
    {
        try
        {
            using var service = new ServiceController(serviceName);
            
            if (service.Status == ServiceControllerStatus.Stopped)
            {
                Console.WriteLine($"Service '{serviceName}' is already stopped");
                return true;
            }
            
            Console.WriteLine($"Stopping service '{serviceName}'...");
            service.Stop();
            service.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(30));
            
            Console.WriteLine($"Service '{serviceName}' stopped successfully");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error stopping service: {ex.Message}");
            return false;
        }
    }
    
    public static void GetServiceStatus(string serviceName)
    {
        try
        {
            using var service = new ServiceController(serviceName);
            
            Console.WriteLine($"Service Name: {service.ServiceName}");
            Console.WriteLine($"Display Name: {service.DisplayName}");
            Console.WriteLine($"Status: {service.Status}");
            Console.WriteLine($"Start Type: {service.StartType}");
        }
        catch (InvalidOperationException)
        {
            Console.WriteLine($"Service '{serviceName}' does not exist in SCM");
            Console.WriteLine("Note: Service may exist in registry but not yet loaded by SCM");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting service status: {ex.Message}");
        }
    }
    
    private const uint DELETE = 0x00010000;
    
    private static bool SetRegistryDWord(IntPtr keyHandle, string valueName, uint value)
    {
        var valueNameUs = NtHelper.InitUnicodeString(valueName);
        
        try
        {
            IntPtr dataPtr = Marshal.AllocHGlobal(4);
            try
            {
                Marshal.WriteInt32(dataPtr, (int)value);
                
                uint status = NtApi.NtSetValueKey(
                    keyHandle,
                    ref valueNameUs,
                    0,
                    NtConstants.REG_DWORD,
                    dataPtr,
                    4);
                
                if (!NtHelper.NT_SUCCESS(status))
                {
                    Console.WriteLine($"Failed to set {valueName}: 0x{status:X8}");
                    return false;
                }
                
                return true;
            }
            finally
            {
                Marshal.FreeHGlobal(dataPtr);
            }
        }
        finally
        {
            NtHelper.FreeUnicodeString(ref valueNameUs);
        }
    }
    
    private static bool SetRegistryString(IntPtr keyHandle, string valueName, string value)
    {
        var valueNameUs = NtHelper.InitUnicodeString(valueName);
        
        try
        {
            var bytes = Encoding.Unicode.GetBytes(value + "\0");
            IntPtr dataPtr = Marshal.AllocHGlobal(bytes.Length);
            
            try
            {
                Marshal.Copy(bytes, 0, dataPtr, bytes.Length);
                
                uint status = NtApi.NtSetValueKey(
                    keyHandle,
                    ref valueNameUs,
                    0,
                    NtConstants.REG_SZ,
                    dataPtr,
                    (uint)bytes.Length);
                
                if (!NtHelper.NT_SUCCESS(status))
                {
                    Console.WriteLine($"Failed to set {valueName}: 0x{status:X8}");
                    return false;
                }
                
                return true;
            }
            finally
            {
                Marshal.FreeHGlobal(dataPtr);
            }
        }
        finally
        {
            NtHelper.FreeUnicodeString(ref valueNameUs);
        }
    }
}
