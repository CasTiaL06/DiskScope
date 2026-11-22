using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace DiskScope.Scanning;

internal static class NativeMethods
{
    public const uint FileShareRead = 0x1;
    public const uint FileShareWrite = 0x2;
    public const uint OpenExisting = 3;

    public const uint IoctlStorageQueryProperty = 0x002D1400;
    public const uint IoctlVolumeGetVolumeDiskExtents = 0x00560000;

    public const int ErrorMoreData = 234;

    [DllImport("kernel32.dll", EntryPoint = "CreateFileW", CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern SafeFileHandle CreateFile(
        string fileName,
        uint desiredAccess,
        uint shareMode,
        IntPtr securityAttributes,
        uint creationDisposition,
        uint flagsAndAttributes,
        IntPtr templateFile);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool DeviceIoControl(
        SafeFileHandle device,
        uint ioControlCode,
        byte[]? inBuffer,
        int inBufferSize,
        byte[] outBuffer,
        int outBufferSize,
        out int bytesReturned,
        IntPtr overlapped);
}
