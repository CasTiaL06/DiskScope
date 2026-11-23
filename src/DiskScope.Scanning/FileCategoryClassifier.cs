namespace DiskScope.Scanning;

/// <summary>Dosya uzantısına bakarak kategori belirler. Bilinmeyen uzantılar "Other" olur.</summary>
public static class FileCategoryClassifier
{
    private static readonly Dictionary<string, FileCategory> Map = Build();

    /// <param name="extension">Noktalı uzantı, örneğin ".mp4" (büyük/küçük harf fark etmez).</param>
    public static FileCategory Classify(string? extension)
    {
        if (string.IsNullOrEmpty(extension))
        {
            return FileCategory.Other;
        }

        return Map.TryGetValue(extension, out var category)
            ? category
            : FileCategory.Other;
    }

    private static Dictionary<string, FileCategory> Build()
    {
        var map = new Dictionary<string, FileCategory>(StringComparer.OrdinalIgnoreCase);

        void Add(FileCategory category, params string[] extensions)
        {
            foreach (var extension in extensions)
            {
                map[extension] = category;
            }
        }

        Add(FileCategory.Video, ".mp4", ".mkv", ".avi", ".mov", ".wmv", ".flv", ".webm", ".m4v", ".mpg", ".mpeg");
        Add(FileCategory.Image, ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".tif", ".tiff", ".webp", ".heic", ".raw", ".cr2", ".nef", ".arw", ".dng", ".psd", ".svg", ".ico");
        Add(FileCategory.Audio, ".mp3", ".wav", ".flac", ".aac", ".ogg", ".m4a", ".wma", ".opus");
        Add(FileCategory.Document, ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx", ".txt", ".rtf", ".odt", ".ods", ".odp", ".csv", ".md", ".epub");
        Add(FileCategory.Archive, ".zip", ".rar", ".7z", ".tar", ".gz", ".bz2", ".xz", ".cab", ".tgz");
        Add(FileCategory.Application, ".exe", ".msi", ".msix", ".appx", ".apk", ".jar", ".bat", ".cmd");
        Add(FileCategory.System, ".dll", ".sys", ".drv", ".ocx", ".cat", ".mui", ".mun");
        Add(FileCategory.DiskImage, ".iso", ".img", ".vhd", ".vhdx", ".vmdk", ".vdi", ".wim", ".esd", ".qcow2", ".ova");

        return map;
    }
}
