namespace DiskScope.Scanning;

public enum DiskHealthStatus
{
    Unknown = 0,
    Healthy = 1,
    Warning = 2,
    Unhealthy = 3
}

/// <summary>Bir fiziksel diskin Windows'tan okunan sağlık bilgisi. Okunamayan alanlar null'dır (uydurulmaz).</summary>
public sealed record PhysicalDiskHealth(
    int DiskNumber,
    string Name,
    DiskMediaType MediaType,
    DiskHealthStatus Status,
    int? TemperatureCelsius,
    long? PowerOnHours,
    long SizeBytes);

/// <summary>Windows Depolama (MSFT_PhysicalDisk) sayısal değerlerini bizim türlerimize çevirir.</summary>
public static class DiskHealthMapper
{
    private const int BusTypeNvme = 17;

    public static DiskHealthStatus MapStatus(int? healthStatus) => healthStatus switch
    {
        0 => DiskHealthStatus.Healthy,
        1 => DiskHealthStatus.Warning,
        2 => DiskHealthStatus.Unhealthy,
        _ => DiskHealthStatus.Unknown
    };

    public static DiskMediaType MapMedia(int? mediaType, int? busType)
    {
        if (busType == BusTypeNvme)
        {
            return DiskMediaType.Nvme;
        }

        return mediaType switch
        {
            3 => DiskMediaType.Hdd,
            4 => DiskMediaType.Ssd,
            _ => DiskMediaType.Unknown
        };
    }
}
