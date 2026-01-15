using System.Runtime.InteropServices;

namespace NtServiceInstaller;

#region NT Structures

[StructLayout(LayoutKind.Sequential)]
public struct UNICODE_STRING
{
    public ushort Length;
    public ushort MaximumLength;
    public IntPtr Buffer;
}

[StructLayout(LayoutKind.Sequential)]
public struct OBJECT_ATTRIBUTES
{
    public int Length;
    public IntPtr RootDirectory;
    public IntPtr ObjectName; // UNICODE_STRING*
    public uint Attributes;
    public IntPtr SecurityDescriptor;
    public IntPtr SecurityQualityOfService;
}

[StructLayout(LayoutKind.Sequential)]
public struct IO_STATUS_BLOCK
{
    public IntPtr Status;
    public IntPtr Information;
}

#endregion

#region Constants

public static class NtConstants
{
    // NTSTATUS codes
    public const uint STATUS_SUCCESS = 0x00000000;
    public const uint STATUS_OBJECT_NAME_NOT_FOUND = 0xC0000034;
    
    // Registry key access rights
    public const uint KEY_QUERY_VALUE = 0x0001;
    public const uint KEY_SET_VALUE = 0x0002;
    public const uint KEY_CREATE_SUB_KEY = 0x0004;
    public const uint KEY_ENUMERATE_SUB_KEYS = 0x0008;
    public const uint KEY_NOTIFY = 0x0010;
    public const uint KEY_CREATE_LINK = 0x0020;
    public const uint KEY_WOW64_64KEY = 0x0100;
    public const uint KEY_WOW64_32KEY = 0x0200;
    
    public const uint KEY_READ = 0x20019;
    public const uint KEY_WRITE = 0x20006;
    public const uint KEY_ALL_ACCESS = 0xF003F;
    public const uint DELETE = 0x00010000;
    
    // Object attributes
    public const uint OBJ_CASE_INSENSITIVE = 0x00000040;
    
    // Disposition values
    public const uint REG_CREATED_NEW_KEY = 0x00000001;
    public const uint REG_OPENED_EXISTING_KEY = 0x00000002;
    
    // Registry value types
    public const uint REG_NONE = 0;
    public const uint REG_SZ = 1;
    public const uint REG_EXPAND_SZ = 2;
    public const uint REG_BINARY = 3;
    public const uint REG_DWORD = 4;
    public const uint REG_DWORD_BIG_ENDIAN = 5;
    public const uint REG_LINK = 6;
    public const uint REG_MULTI_SZ = 7;
    public const uint REG_QWORD = 11;
    
    // Service registry values
    public const uint SERVICE_WIN32_OWN_PROCESS = 0x00000010;
    public const uint SERVICE_AUTO_START = 0x00000002;
    public const uint SERVICE_DEMAND_START = 0x00000003;
    public const uint SERVICE_ERROR_NORMAL = 0x00000001;
    
    // Registry root
    public const string SERVICES_KEY = @"\Registry\Machine\SYSTEM\CurrentControlSet\Services";
}

#endregion

#region Helper Methods

public static class NtHelper
{
    public static UNICODE_STRING InitUnicodeString(string str)
    {
        var unicodeString = new UNICODE_STRING();
        if (string.IsNullOrEmpty(str))
        {
            unicodeString.Length = 0;
            unicodeString.MaximumLength = 0;
            unicodeString.Buffer = IntPtr.Zero;
        }
        else
        {
            unicodeString.Length = (ushort)(str.Length * 2);
            unicodeString.MaximumLength = (ushort)((str.Length * 2) + 2);
            unicodeString.Buffer = Marshal.StringToHGlobalUni(str);
        }
        return unicodeString;
    }
    
    public static void FreeUnicodeString(ref UNICODE_STRING unicodeString)
    {
        if (unicodeString.Buffer != IntPtr.Zero)
        {
            Marshal.FreeHGlobal(unicodeString.Buffer);
            unicodeString.Buffer = IntPtr.Zero;
        }
    }
    
    public static OBJECT_ATTRIBUTES InitObjectAttributes(
        ref UNICODE_STRING objectName,
        uint attributes = NtConstants.OBJ_CASE_INSENSITIVE,
        IntPtr rootDirectory = default)
    {
        var oa = new OBJECT_ATTRIBUTES();
        oa.Length = Marshal.SizeOf<OBJECT_ATTRIBUTES>();
        oa.RootDirectory = rootDirectory;
        oa.Attributes = attributes;
        oa.SecurityDescriptor = IntPtr.Zero;
        oa.SecurityQualityOfService = IntPtr.Zero;
        
        // Pin the UNICODE_STRING
        var handle = GCHandle.Alloc(objectName, GCHandleType.Pinned);
        oa.ObjectName = handle.AddrOfPinnedObject();
        
        return oa;
    }
    
    public static bool NT_SUCCESS(uint status)
    {
        return status == NtConstants.STATUS_SUCCESS;
    }
}

#endregion
