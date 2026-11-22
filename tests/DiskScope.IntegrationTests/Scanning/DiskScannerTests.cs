
using DiskScope.Scanning;

namespace DiskScope.IntegrationTests.Scanning;

public sealed class DiskScannerTests
{
    [Fact]
    public void GetDisks_ReturnsReadyDrives()
    {
        var scanner = new DiskScanner();

        var disks = scanner.GetDisks();

        Assert.NotNull(disks);

        Assert.All(disks, disk =>
        {
            Assert.False(string.IsNullOrWhiteSpace(disk.Name));
            Assert.False(string.IsNullOrWhiteSpace(disk.DriveLetter));
            Assert.True(disk.TotalSize > 0);
            Assert.True(disk.AvailableFreeSpace >= 0);
            Assert.True(disk.UsedSpace >= 0);
        });
    }
}
