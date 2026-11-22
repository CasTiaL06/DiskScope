namespace DiskScope.Scanning;

/// <summary>
/// Windows'tan okunan ham bilgilere bakarak disk türüne karar verir.
/// Hiçbir Windows çağrısı yapmaz; bu yüzden kolayca test edilebilir.
/// Emin olunamayan her durumda Unknown döner (yanlış bilgi göstermemek için).
/// </summary>
public static class DiskMediaTypeClassifier
{
    public static DiskMediaType Classify(
        int diskExtentCount,
        StorageBusType busType,
        bool? incursSeekPenalty)
    {
        // Birden fazla fiziksel diske yayılan (veya okunamayan) birimlerde tek bir tür söylenemez.
        if (diskExtentCount != 1)
        {
            return DiskMediaType.Unknown;
        }

        switch (busType)
        {
            case StorageBusType.Nvme:
                return DiskMediaType.Nvme;

            // Yalnızca doğrudan bağlı klasik disk yollarında "seek penalty" bilgisine güveniyoruz.
            case StorageBusType.Sata:
            case StorageBusType.Ata:
            case StorageBusType.Sas:
                return incursSeekPenalty switch
                {
                    true => DiskMediaType.Hdd,
                    false => DiskMediaType.Ssd,
                    null => DiskMediaType.Unknown
                };

            // USB kutular, RAID, sanal diskler, Storage Spaces vb. güvenilir bilgi vermeyebilir.
            default:
                return DiskMediaType.Unknown;
        }
    }
}
