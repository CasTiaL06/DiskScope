using DiskScope.Scanning;

namespace DiskScope.IntegrationTests.Health;

public sealed class DiskHealthMapperTests
{
    [Theory]
    [InlineData(0, DiskHealthStatus.Healthy)]
    [InlineData(1, DiskHealthStatus.Warning)]
    [InlineData(2, DiskHealthStatus.Unhealthy)]
    [InlineData(5, DiskHealthStatus.Unknown)]
    [InlineData(null, DiskHealthStatus.Unknown)]
    public void MapStatus_ReturnsExpected(int? value, DiskHealthStatus expected)
    {
        Assert.Equal(expected, DiskHealthMapper.MapStatus(value));
    }

    [Theory]
    [InlineData(3, 11, DiskMediaType.Hdd)]
    [InlineData(4, 11, DiskMediaType.Ssd)]
    [InlineData(4, 17, DiskMediaType.Nvme)]
    [InlineData(0, 17, DiskMediaType.Nvme)]
    [InlineData(0, 7, DiskMediaType.Unknown)]
    [InlineData(null, null, DiskMediaType.Unknown)]
    public void MapMedia_ReturnsExpected(int? media, int? bus, DiskMediaType expected)
    {
        Assert.Equal(expected, DiskHealthMapper.MapMedia(media, bus));
    }

    [Fact]
    public void DiskHealthService_NeverThrows()
    {
        var result = new DiskHealthService().GetAll();

        Assert.NotNull(result);
    }

    [Fact]
    public void PhysicalDiskLocator_InvalidRoot_ReturnsNull()
    {
        Assert.Null(PhysicalDiskLocator.GetDiskNumber(""));
        Assert.Null(PhysicalDiskLocator.GetDiskNumber("1:\\"));
    }
}
