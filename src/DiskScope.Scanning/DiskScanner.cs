namespace DiskScope.Scanning;

public sealed class DiskScanner
{
    private readonly IDiskMediaTypeDetector _mediaTypeDetector;

    public DiskScanner() : this(new WindowsDiskMediaTypeDetector())
    {
    }

    public DiskScanner(IDiskMediaTypeDetector mediaTypeDetector)
    {
        ArgumentNullException.ThrowIfNull(mediaTypeDetector);

        _mediaTypeDetector = mediaTypeDetector;
    }

    public IReadOnlyList<DiskInfo> GetDisks()
    {
        var disks = new List<DiskInfo>();

        foreach (var drive in DriveInfo.GetDrives())
        {
            try
            {
                if (!drive.IsReady)
                {
                    continue;
                }

                // Disk türünü yalnızca yerel (sabit) disklerde soruyoruz.
                // CD, ağ sürücüsü, çıkarılabilir disk vb. için Unknown kalır.
                var mediaType = drive.DriveType == DriveType.Fixed
                    ? _mediaTypeDetector.Detect(drive.Name)
                    : DiskMediaType.Unknown;

                disks.Add(new DiskInfo
                {
                    Name = string.IsNullOrWhiteSpace(drive.VolumeLabel)
                        ? "Local Disk"
                        : drive.VolumeLabel,

                    DriveLetter = drive.Name,

                    DriveType = drive.DriveType.ToString(),

                    MediaType = mediaType,

                    FileSystem = drive.DriveFormat,

                    TotalSize = drive.TotalSize,

                    AvailableFreeSpace = drive.AvailableFreeSpace
                });
            }
            catch (IOException)
            {
                // Skip drives that become unavailable during scanning.
            }
            catch (UnauthorizedAccessException)
            {
                // Skip drives that cannot be accessed.
            }
        }

        return disks;
    }
}
