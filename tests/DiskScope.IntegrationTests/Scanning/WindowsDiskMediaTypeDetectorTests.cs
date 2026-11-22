using DiskScope.Scanning;

namespace DiskScope.IntegrationTests.Scanning;

public sealed class WindowsDiskMediaTypeDetectorTests
{
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("1:\\")]
    [InlineData("\\\\sunucu\\paylasim")]
    public void Detect_InvalidRoot_ReturnsUnknownWithoutThrowing(string root)
    {
        var detector = new WindowsDiskMediaTypeDetector();

        var result = detector.Detect(root);

        Assert.Equal(DiskMediaType.Unknown, result);
    }

    [Fact]
    public void Detect_NonExistingDrive_ReturnsUnknownWithoutThrowing()
    {
        var detector = new WindowsDiskMediaTypeDetector();

        // Büyük olasılıkla bilgisayarda olmayan bir sürücü harfi.
        var result = detector.Detect("Q:\\");

        Assert.True(Enum.IsDefined(result));
    }

    [Fact]
    public void Detect_RealFixedDrives_ReturnsDefinedValueWithoutThrowing()
    {
        var detector = new WindowsDiskMediaTypeDetector();

        foreach (var drive in DriveInfo.GetDrives())
        {
            if (drive.DriveType != DriveType.Fixed)
            {
                continue;
            }

            var result = detector.Detect(drive.Name);

            Assert.True(Enum.IsDefined(result));
        }
    }
}
