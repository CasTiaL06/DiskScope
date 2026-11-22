using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace DiskScope.Scanning;

/// <summary>
/// Sürücü harfinden fiziksel disk türünü (SSD / HDD / NVMe) Windows'a sorarak bulur.
/// Yönetici izni gerektirmez (diskin içeriğini okumaz, yalnızca özelliklerini sorar).
/// Bir sorun olursa istisna fırlatmaz, Unknown döner.
/// </summary>
public sealed class WindowsDiskMediaTypeDetector : IDiskMediaTypeDetector
{
    private const int StorageDeviceProperty = 0;
    private const int StorageDeviceSeekPenaltyProperty = 7;

    // STORAGE_DEVICE_DESCRIPTOR içinde BusType alanının yeri (bayt cinsinden).
    private const int BusTypeOffset = 28;

    // DEVICE_SEEK_PENALTY_DESCRIPTOR içinde IncursSeekPenalty alanının yeri.
    private const int SeekPenaltyOffset = 8;

    public DiskMediaType Detect(string driveRoot)
    {
        if (!OperatingSystem.IsWindows())
        {
            return DiskMediaType.Unknown;
        }

        if (string.IsNullOrWhiteSpace(driveRoot) || !char.IsAsciiLetter(driveRoot[0]))
        {
            return DiskMediaType.Unknown;
        }

        try
        {
            var devicePath = $@"\\.\{char.ToUpperInvariant(driveRoot[0])}:";

            // Erişim hakkı 0: yalnızca sorgu yapıyoruz, diske okuma/yazma yapmıyoruz.
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
                return DiskMediaType.Unknown;
            }

            var extentCount = QueryDiskExtentCount(handle);
            var busType = QueryBusType(handle);
            var seekPenalty = QuerySeekPenalty(handle);

            return DiskMediaTypeClassifier.Classify(
                extentCount,
                busType ?? StorageBusType.Unknown,
                seekPenalty);
        }
        catch (Exception)
        {
            // Tespit hiçbir zaman uygulamayı çökertmemeli.
            return DiskMediaType.Unknown;
        }
    }

    private static int QueryDiskExtentCount(SafeFileHandle handle)
    {
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
            return -1;
        }

        return returned >= 4 ? BitConverter.ToInt32(output, 0) : -1;
    }

    private static StorageBusType? QueryBusType(SafeFileHandle handle)
    {
        var output = QueryStorageProperty(handle, StorageDeviceProperty, 1024, out var returned);

        if (output is null || returned < BusTypeOffset + 4)
        {
            return null;
        }

        return (StorageBusType)BitConverter.ToUInt32(output, BusTypeOffset);
    }

    private static bool? QuerySeekPenalty(SafeFileHandle handle)
    {
        var output = QueryStorageProperty(handle, StorageDeviceSeekPenaltyProperty, 64, out var returned);

        if (output is null || returned <= SeekPenaltyOffset)
        {
            return null;
        }

        return output[SeekPenaltyOffset] != 0;
    }

    private static byte[]? QueryStorageProperty(
        SafeFileHandle handle,
        int propertyId,
        int outputSize,
        out int returned)
    {
        // STORAGE_PROPERTY_QUERY: PropertyId (4 bayt) + QueryType (4 bayt, 0 = standart sorgu) + 4 bayt boşluk.
        var query = new byte[12];
        BitConverter.TryWriteBytes(query.AsSpan(0, 4), propertyId);

        var output = new byte[outputSize];

        var ok = NativeMethods.DeviceIoControl(
            handle,
            NativeMethods.IoctlStorageQueryProperty,
            query,
            query.Length,
            output,
            output.Length,
            out returned,
            IntPtr.Zero);

        return ok ? output : null;
    }
}
