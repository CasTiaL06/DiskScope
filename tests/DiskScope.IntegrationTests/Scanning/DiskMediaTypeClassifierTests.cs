using DiskScope.Scanning;

namespace DiskScope.IntegrationTests.Scanning;

public sealed class DiskMediaTypeClassifierTests
{
    [Theory]
    [InlineData(1, StorageBusType.Nvme, null, DiskMediaType.Nvme)]
    [InlineData(1, StorageBusType.Nvme, false, DiskMediaType.Nvme)]
    [InlineData(1, StorageBusType.Sata, false, DiskMediaType.Ssd)]
    [InlineData(1, StorageBusType.Sata, true, DiskMediaType.Hdd)]
    [InlineData(1, StorageBusType.Ata, false, DiskMediaType.Ssd)]
    [InlineData(1, StorageBusType.Sas, true, DiskMediaType.Hdd)]
    public void Classify_ReliableCases_ReturnsExpectedType(
        int extentCount,
        StorageBusType busType,
        bool? seekPenalty,
        DiskMediaType expected)
    {
        var result = DiskMediaTypeClassifier.Classify(extentCount, busType, seekPenalty);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(1, StorageBusType.Sata, null)]            // Seek penalty bilgisi okunamadı
    [InlineData(1, StorageBusType.Usb, false)]            // USB kutular güvenilir değil
    [InlineData(1, StorageBusType.Raid, false)]           // RAID
    [InlineData(1, StorageBusType.Virtual, false)]        // Sanal disk
    [InlineData(1, StorageBusType.FileBackedVirtual, false)] // VHD/VHDX
    [InlineData(1, StorageBusType.StorageSpaces, false)]  // Storage Spaces
    [InlineData(1, StorageBusType.Unknown, false)]        // Yol bilinmiyor
    [InlineData(0, StorageBusType.Nvme, false)]           // Disk bilgisi yok
    [InlineData(-1, StorageBusType.Sata, false)]          // Sorgu başarısız
    [InlineData(2, StorageBusType.Sata, false)]           // Birden fazla diske yayılmış birim
    public void Classify_UncertainCases_ReturnsUnknown(
        int extentCount,
        StorageBusType busType,
        bool? seekPenalty)
    {
        var result = DiskMediaTypeClassifier.Classify(extentCount, busType, seekPenalty);

        Assert.Equal(DiskMediaType.Unknown, result);
    }
}
