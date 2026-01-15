using System.Security.Principal;
using WindowsServiceInstaller;

class Program
{
    static int Main(string[] args)
    {
        if (!IsAdministrator())
        {
            Console.WriteLine("ERROR: This program must be run as Administrator.");
            Console.WriteLine("Please run this application with administrator privileges.");
            return 1;
        }

        if (args.Length == 0)
        {
            ShowHelp();
            return 0;
        }

        var command = args[0].ToLower();

        try
        {
            switch (command)
            {
                case "install":
                    return HandleInstall(args);
                
                case "uninstall":
                    return HandleUninstall(args);
                
                case "start":
                    return HandleStart(args);
                
                case "stop":
                    return HandleStop(args);
                
                case "status":
                    return HandleStatus(args);
                
                case "help":
                case "-h":
                case "--help":
                case "?":
                    ShowHelp();
                    return 0;
                
                default:
                    Console.WriteLine($"Unknown command: {command}");
                    Console.WriteLine();
                    ShowHelp();
                    return 1;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            return 1;
        }
    }

    static int HandleInstall(string[] args)
    {
        if (args.Length < 3)
        {
            Console.WriteLine("ERROR: install command requires at least 2 arguments.");
            Console.WriteLine("Usage: install <exe-path> <service-name> [display-name] [description]");
            return 1;
        }

        var exePath = args[1];
        var serviceName = args[2];
        var displayName = args.Length > 3 ? args[3] : null;
        var description = args.Length > 4 ? args[4] : null;

        var success = ServiceInstaller.InstallService(exePath, serviceName, displayName, description);
        return success ? 0 : 1;
    }

    static int HandleUninstall(string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine("ERROR: uninstall command requires service name.");
            Console.WriteLine("Usage: uninstall <service-name>");
            return 1;
        }

        var serviceName = args[1];
        var success = ServiceInstaller.UninstallService(serviceName);
        return success ? 0 : 1;
    }

    static int HandleStart(string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine("ERROR: start command requires service name.");
            Console.WriteLine("Usage: start <service-name>");
            return 1;
        }

        var serviceName = args[1];
        var success = ServiceInstaller.StartService(serviceName);
        return success ? 0 : 1;
    }

    static int HandleStop(string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine("ERROR: stop command requires service name.");
            Console.WriteLine("Usage: stop <service-name>");
            return 1;
        }

        var serviceName = args[1];
        var success = ServiceInstaller.StopService(serviceName);
        return success ? 0 : 1;
    }

    static int HandleStatus(string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine("ERROR: status command requires service name.");
            Console.WriteLine("Usage: status <service-name>");
            return 1;
        }

        var serviceName = args[1];
        ServiceInstaller.GetServiceStatus(serviceName);
        return 0;
    }

    static void ShowHelp()
    {
        Console.WriteLine("Windows Service Installer - Standalone Executable");
        Console.WriteLine("================================================");
        Console.WriteLine();
        Console.WriteLine("USAGE:");
        Console.WriteLine("  WindowsServiceInstaller.exe <command> [arguments]");
        Console.WriteLine();
        Console.WriteLine("COMMANDS:");
        Console.WriteLine("  install <exe-path> <service-name> [display-name] [description]");
        Console.WriteLine("      Install an executable as a Windows service");
        Console.WriteLine("      - exe-path: Full path to the executable file");
        Console.WriteLine("      - service-name: Name for the service (no spaces)");
        Console.WriteLine("      - display-name: (Optional) Display name for the service");
        Console.WriteLine("      - description: (Optional) Service description");
        Console.WriteLine();
        Console.WriteLine("  uninstall <service-name>");
        Console.WriteLine("      Uninstall a Windows service");
        Console.WriteLine();
        Console.WriteLine("  start <service-name>");
        Console.WriteLine("      Start a Windows service");
        Console.WriteLine();
        Console.WriteLine("  stop <service-name>");
        Console.WriteLine("      Stop a Windows service");
        Console.WriteLine();
        Console.WriteLine("  status <service-name>");
        Console.WriteLine("      Check the status of a Windows service");
        Console.WriteLine();
        Console.WriteLine("  help");
        Console.WriteLine("      Show this help message");
        Console.WriteLine();
        Console.WriteLine("EXAMPLES:");
        Console.WriteLine("  WindowsServiceInstaller.exe install \"C:\\MyApp\\app.exe\" MyAppService \"My Application\" \"My app description\"");
        Console.WriteLine("  WindowsServiceInstaller.exe start MyAppService");
        Console.WriteLine("  WindowsServiceInstaller.exe status MyAppService");
        Console.WriteLine("  WindowsServiceInstaller.exe stop MyAppService");
        Console.WriteLine("  WindowsServiceInstaller.exe uninstall MyAppService");
        Console.WriteLine();
        Console.WriteLine("NOTE: This program must be run as Administrator.");
    }

    static bool IsAdministrator()
    {
        try
        {
            using var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
        catch
        {
            return false;
        }
    }
}
