namespace DiskScope.Scanning;

public enum ScanIssueKind
{
    AccessDenied,
    IoError,
    SkippedLink
}

/// <summary>Taramada atlanan veya okunamayan bir yol.</summary>
public sealed record ScanIssue(string Path, ScanIssueKind Kind, string Message);

/// <summary>Tarama sırasında periyodik olarak bildirilen ilerleme bilgisi.</summary>
public sealed record ScanProgress(long Files, long Folders, long Bytes, TimeSpan Elapsed, string CurrentPath);

public sealed record ExtensionStat(string Extension, FileCategory Category, long Size, long FileCount);

public sealed record CategoryStat(FileCategory Category, long Size, long FileCount);

public sealed class ScanOptions
{
    /// <summary>Bu boyutun altındaki dosyalar klasör başına tek bir "küçük dosyalar" düğümünde toplanır.</summary>
    public long MinFileNodeSize { get; init; } = 256 * 1024;

    public int MaxDegreeOfParallelism { get; init; } = Math.Clamp(Environment.ProcessorCount, 1, 8);
}

public sealed class ScanResult
{
    public ScanResult(
        string rootPath,
        ScanNode root,
        IReadOnlyList<ExtensionStat> extensions,
        IReadOnlyList<CategoryStat> categories,
        IReadOnlyList<ScanIssue> issues,
        long issueCount,
        TimeSpan elapsed)
    {
        RootPath = rootPath;
        Root = root;
        Extensions = extensions;
        Categories = categories;
        Issues = issues;
        IssueCount = issueCount;
        Elapsed = elapsed;
    }

    public string RootPath { get; }

    public ScanNode Root { get; }

    public long TotalSize => Root.Size;

    public long TotalFiles => Root.FileCount;

    public long TotalFolders => Root.FolderCount;

    public IReadOnlyList<ExtensionStat> Extensions { get; }

    public IReadOnlyList<CategoryStat> Categories { get; }

    /// <summary>Atlanan/okunamayan yolların ilk birkaç yüzü.</summary>
    public IReadOnlyList<ScanIssue> Issues { get; }

    /// <summary>Atlanan/okunamayan yolların gerçek toplam sayısı.</summary>
    public long IssueCount { get; }

    public TimeSpan Elapsed { get; }
}
