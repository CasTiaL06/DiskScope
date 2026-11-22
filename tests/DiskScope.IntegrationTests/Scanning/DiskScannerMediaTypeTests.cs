using DiskScope.Scanning;

namespace DiskScope.IntegrationTests.Scanning;

public sealed class DiskScannerMediaTypeTests
{
    private sealed class FakeDetector : IDiskMediaTypeDetector
    {
        public DiskMediaType Detect(string driveRoot) => DiskMediaType.Ssd;
    }

    [Fact]
    public void GetDisks_UsesDetectorOnlyForFixedDrives()
    {
        var scanner = new DiskScanner(new FakeDetector());

        var disks = scanner.GetDisks();

        Assert.All(disks, disk =>
        {
            var expected = disk.DriveType == nameof(DriveType.Fixed)
                ? DiskMediaType.Ssd
                : DiskMediaType.Unknown;

            Assert.Equal(expected, disk.MediaType);
        });
    }

    [Fact]
    public void DiskInfo_MediaType_DefaultsToUnknown()
    {
        var disk = new DiskInfo();

        Assert.Equal(DiskMediaType.Unknown, disk.MediaType);
    }
}
