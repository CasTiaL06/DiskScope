using System.Runtime.InteropServices;

namespace DiskScope.Scanning;

/// <summary>Bir sürücü harfinin (örneğin "C:\") hangi fiziksel diskte olduğunu bulur.</summary>
public static class PhysicalDiskLocator
{
    /// <summary>
    /// Disk numarasını döndürür. Birim birden fazla diske yayılmışsa veya bulunamazsa null döner (tahmin yapılmaz).
    /// </summary>
    public static int? GetDiskNumber(string driveRoot)
    {
        if (!OperatingSystem.IsWindows() ||
            string.IsNullOrWhiteSpace(driveRoot) ||
            !char.IsAsciiLetter(driveRoot[0]))
        {
            return null;
        }

        try
        {
            var devicePath = $@"\\.\{char.ToUpperInvariant(driveRoot[0])}:";

            using var handle = NativeMethods.CreateFile(
                devicePath,
                0,
                NativeMethods.FileShareRead | NativeMethods.FileShareWrite,
                IntPtr.Zero,
                NativeMethods.OpenExisting,
                0,
                IntPtr.Zero);

            if (handle.IsInvalid)
            {
                return null;
            }

            var output = new byte[4096];

            var ok = NativeMethods.DeviceIoControl(
                handle,
                NativeMethods.IoctlVolumeGetVolumeDiskExtents,
                null,
                0,
                output,
                output.Length,
                out var returned,
                IntPtr.Zero);

            if (!ok && Marshal.GetLastWin32Error() != NativeMethods.ErrorMoreData)
            {
                return null;
            }

            // VOLUME_DISK_EXTENTS: ilk 4 bayt uzantı sayısı, 8. bayttan itibaren ilk DISK_EXTENT (DiskNumber).
            if (returned < 12 || BitConverter.ToInt32(output, 0) != 1)
            {
                return null;
            }

            return BitConverter.ToInt32(output, 8);
        }
        catch (Exception)
        {
            return null;
        }
    }
}
