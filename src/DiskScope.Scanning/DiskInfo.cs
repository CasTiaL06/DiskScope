
namespace DiskScope.Scanning;

public sealed class DiskInfo
{
    public string Name { get; init; } = string.Empty;

    public string DriveLetter { get; init; } = string.Empty;

    public string DriveType { get; init; } = string.Empty;

    public DiskMediaType MediaType { get; init; } = DiskMediaType.Unknown;

    public string FileSystem { get; init; } = string.Empty;

    /// <summary>Sürücünün bulunduğu fiziksel disk numarası (belirlenemezse null).</summary>
    public int? DiskNumber { get; init; }

    public long TotalSize { get; init; }

    public long AvailableFreeSpace { get; init; }

    public long UsedSpace => TotalSize - AvailableFreeSpace;

    public double UsagePercentage => TotalSize <= 0
    ? 0
    : (double)UsedSpace / TotalSize * 100;

    public bool IsWarning => UsagePercentage >= 75 && UsagePercentage < 90;

    public bool IsCritical => UsagePercentage >= 90;

    public string TotalSizeFormatted => FormatSize(TotalSize);

    public string UsedSpaceFormatted => FormatSize(UsedSpace);

    public string AvailableFreeSpaceFormatted => FormatSize(AvailableFreeSpace);

    private static string FormatSize(long bytes)
    {
        string[] units = { "B", "KB", "MB", "GB", "TB" };

        double size = bytes;
        int unitIndex = 0;

        while (size >= 1024 && unitIndex < units.Length - 1)
        {
            size /= 1024;
            unitIndex++;
        }

        return $"{size:0.##} {units[unitIndex]}";
    }
}
