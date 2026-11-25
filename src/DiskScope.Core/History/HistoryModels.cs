namespace DiskScope.Core.History;

/// <summary>Bir klasörün (taramanın kökündeki) boyut bilgisi.</summary>
public sealed class FolderUsage
{
    public string Name { get; set; } = string.Empty;

    public long Size { get; set; }
}

/// <summary>Bir dosya kategorisinin toplam boyutu. Category, FileCategory enum adıdır (örneğin "Video").</summary>
public sealed class CategoryUsage
{
    public string Category { get; set; } = string.Empty;

    public long Size { get; set; }

    public long FileCount { get; set; }
}

/// <summary>Tamamlanan bir taramanın özeti (geçmiş ve Genel Bakış ekranı için kalıcı olarak saklanır).</summary>
public sealed class ScanSummary
{
    public string Path { get; set; } = string.Empty;

    public DateTime ScannedAtUtc { get; set; }

    public long TotalSize { get; set; }

    public long TotalFiles { get; set; }

    public long TotalFolders { get; set; }

    public double ElapsedSeconds { get; set; }

    public List<FolderUsage> TopFolders { get; set; } = new();

    public List<CategoryUsage> Categories { get; set; } = new();
}

/// <summary>Bir günün disk doluluk kaydı (tüm sabit diskler toplamı).</summary>
public sealed class UsageSnapshot
{
    /// <summary>Yerel tarih, "yyyy-MM-dd" biçiminde.</summary>
    public string Day { get; set; } = string.Empty;

    public long UsedBytes { get; set; }

    public long TotalBytes { get; set; }

    public double UsedPercent => TotalBytes > 0 ? UsedBytes * 100.0 / TotalBytes : 0;
}
