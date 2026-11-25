using DiskScope.Core.History;

namespace DiskScope.Scanning;

/// <summary>Tamamlanan bir taramadan kalıcı saklanacak özeti üretir.</summary>
public static class ScanSummaryFactory
{
    public static ScanSummary Create(ScanResult result, DateTime scannedAtUtc, int topFolderCount = 5)
    {
        ArgumentNullException.ThrowIfNull(result);

        return new ScanSummary
        {
            Path = result.RootPath,
            ScannedAtUtc = scannedAtUtc,
            TotalSize = result.TotalSize,
            TotalFiles = result.TotalFiles,
            TotalFolders = result.TotalFolders,
            ElapsedSeconds = result.Elapsed.TotalSeconds,

            // Alt öğeler zaten büyükten küçüğe sıralıdır.
            TopFolders = result.Root.Children
                .Where(child => child.IsDirectory && child.Size > 0)
                .Take(topFolderCount)
                .Select(child => new FolderUsage { Name = child.Name, Size = child.Size })
                .ToList(),

            Categories = result.Categories
                .Where(category => category.Size > 0)
                .Select(category => new CategoryUsage
                {
                    Category = category.Category.ToString(),
                    Size = category.Size,
                    FileCount = category.FileCount
                })
                .ToList()
        };
    }
}
