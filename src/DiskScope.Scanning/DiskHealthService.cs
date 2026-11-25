using System.Management;
using System.Runtime.Versioning;

namespace DiskScope.Scanning;

/// <summary>
/// Windows Depolama WMI (MSFT_PhysicalDisk) üzerinden fiziksel disklerin sağlık durumunu okur.
/// Sıcaklık ve çalışma süresi çoğu sürücüde yönetici izni gerektirir; okunamazsa null kalır.
/// Hiçbir hata uygulamayı durdurmaz; bilgi alınamazsa boş liste döner.
/// </summary>
public sealed class DiskHealthService
{
    public IReadOnlyList<PhysicalDiskHealth> GetAll()
    {
        if (!OperatingSystem.IsWindows())
        {
            return Array.Empty<PhysicalDiskHealth>();
        }

        try
        {
            return Query();
        }
        catch (Exception)
        {
            return Array.Empty<PhysicalDiskHealth>();
        }
    }

    [SupportedOSPlatform("windows")]
    private static List<PhysicalDiskHealth> Query()
    {
        var result = new List<PhysicalDiskHealth>();

        var scope = new ManagementScope(@"\\.\root\Microsoft\Windows\Storage");
        scope.Connect();

        using var searcher = new ManagementObjectSearcher(scope, new ObjectQuery("SELECT * FROM MSFT_PhysicalDisk"));

        foreach (ManagementObject disk in searcher.Get())
        {
            using (disk)
            {
                if (!int.TryParse(Text(disk, "DeviceId"), out var diskNumber))
                {
                    continue;
                }

                var temperature = (int?)null;
                var powerOnHours = (long?)null;

                try
                {
                    foreach (ManagementObject counter in disk.GetRelated("MSFT_StorageReliabilityCounter"))
                    {
                        using (counter)
                        {
                            temperature = ToInt(counter["Temperature"]);
                            powerOnHours = ToLong(counter["PowerOnHours"]);
                        }

                        break;
                    }
                }
                catch (ManagementException)
                {
                    // Güvenilirlik sayaçları okunamadı (genellikle yönetici izni gerekir).
                }

                // Sıcaklık 0 veya mantıksız ise sürücü bilgi vermemiş demektir.
                if (temperature is <= 0 or > 150)
                {
                    temperature = null;
                }

                result.Add(new PhysicalDiskHealth(
                    diskNumber,
                    Text(disk, "FriendlyName") ?? $"Disk {diskNumber}",
                    DiskHealthMapper.MapMedia(ToInt(disk["MediaType"]), ToInt(disk["BusType"])),
                    DiskHealthMapper.MapStatus(ToInt(disk["HealthStatus"])),
                    temperature,
                    powerOnHours,
                    ToLong(disk["Size"]) ?? 0));
            }
        }

        return result.OrderBy(d => d.DiskNumber).ToList();
    }

    [SupportedOSPlatform("windows")]
    private static string? Text(ManagementBaseObject obj, string property) => obj[property]?.ToString();

    private static int? ToInt(object? value)
    {
        try
        {
            return value is null ? null : Convert.ToInt32(value);
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static long? ToLong(object? value)
    {
        try
        {
            return value is null ? null : Convert.ToInt64(value);
        }
        catch (Exception)
        {
            return null;
        }
    }
}
