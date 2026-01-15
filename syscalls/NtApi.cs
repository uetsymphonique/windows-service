using System.Runtime.InteropServices;

namespace NtServiceInstaller;

public static class NtApi
{
    private const string NTDLL = "ntdll.dll";
    
    [DllImport(NTDLL, SetLastError = false)]
    public static extern uint NtCreateKey(
        out IntPtr KeyHandle,
        uint DesiredAccess,
        ref OBJECT_ATTRIBUTES ObjectAttributes,
        uint TitleIndex,
        IntPtr Class,
        uint CreateOptions,
        out uint Disposition);
    
    [DllImport(NTDLL, SetLastError = false)]
    public static extern uint NtOpenKey(
        out IntPtr KeyHandle,
        uint DesiredAccess,
        ref OBJECT_ATTRIBUTES ObjectAttributes);
    
    [DllImport(NTDLL, SetLastError = false)]
    public static extern uint NtSetValueKey(
        IntPtr KeyHandle,
        ref UNICODE_STRING ValueName,
        uint TitleIndex,
        uint Type,
        IntPtr Data,
        uint DataSize);
    
    [DllImport(NTDLL, SetLastError = false)]
    public static extern uint NtQueryValueKey(
        IntPtr KeyHandle,
        ref UNICODE_STRING ValueName,
        int KeyValueInformationClass,
        IntPtr KeyValueInformation,
        uint Length,
        out uint ResultLength);
    
    [DllImport(NTDLL, SetLastError = false)]
    public static extern uint NtDeleteKey(IntPtr KeyHandle);
    
    [DllImport(NTDLL, SetLastError = false)]
    public static extern uint NtDeleteValueKey(
        IntPtr KeyHandle,
        ref UNICODE_STRING ValueName);
    
    [DllImport(NTDLL, SetLastError = false)]
    public static extern uint NtClose(IntPtr Handle);
    
    [DllImport(NTDLL, SetLastError = false)]
    public static extern void RtlInitUnicodeString(
        out UNICODE_STRING DestinationString,
        [MarshalAs(UnmanagedType.LPWStr)] string SourceString);
}
