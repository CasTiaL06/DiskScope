namespace DiskScope.Scanning;

/// <summary>
/// Fiziksel diskin türü. Güvenilir şekilde belirlenemezse Unknown kullanılır.
/// </summary>
public enum DiskMediaType
{
    Unknown = 0,
    Hdd = 1,
    Ssd = 2,
    Nvme = 3
}
