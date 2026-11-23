using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO.Enumeration;

namespace DiskScope.Scanning;

/// <summary>
/// Bir disk veya klasörü tarayıp boyut ağacı üretir.
/// Arka planda çalışır, iptal edilebilir, erişilemeyen klasörlerde durmaz (rapor eder).
/// Klasör bağlantıları (junction/symlink) döngü ve çift sayımı önlemek için takip edilmez.
/// </summary>
public sealed class FolderScanner
{
    private const int MaxIssuesKept = 500;

    private static readonly EnumerationOptions EnumOptions = new()
    {
        RecurseSubdirectories = false,
        IgnoreInaccessible = false,   // Erişim hatalarını yakalayıp raporlamak için
        AttributesToSkip = 0,         // Gizli ve sistem dosyaları da sayılsın
        ReturnSpecialDirectories = false
    };

    public Task<ScanResult> ScanAsync(
        string rootPath,
        ScanOptions? options = null,
        IProgress<ScanProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rootPath);

        var effectiveOptions = options ?? new ScanOptions();

        return Task.Run(
            () => Scan(rootPath, effectiveOptions, progress, cancellationToken),
            cancellationToken);
    }

    private static ScanResult Scan(
        string rootPath,
        ScanOptions options,
        IProgress<ScanProgress>? progress,
        CancellationToken cancellationToken)
    {
        var fullRoot = Path.GetFullPath(rootPath);

        if (!Directory.Exists(fullRoot))
        {
            throw new DirectoryNotFoundException($"Folder not found: {fullRoot}");
        }

        var context = new ScanContext(options, progress, cancellationToken);
        var root = new ScanNode(fullRoot, true, null);
        var accumulator = new ScanAccumulator();

        ScanDirectory(fullRoot, root, accumulator, context, parallelChildren: true);

        context.Stopwatch.Stop();

        var extensions = accumulator.Extensions
            .Select(pair => new ExtensionStat(
                pair.Key.ToLowerInvariant(),
                FileCategoryClassifier.Classify(pair.Key),
                pair.Value.Size,
                pair.Value.Count))
            .OrderByDescending(stat => stat.Size)
            .ToList();

        var categories = extensions
            .GroupBy(stat => stat.Category)
            .Select(group => new CategoryStat(
                group.Key,
                group.Sum(stat => stat.Size),
                group.Sum(stat => stat.FileCount)))
            .OrderByDescending(stat => stat.Size)
            .ToList();

        return new ScanResult(
            fullRoot,
            root,
            extensions,
            categories,
            context.Issues,
            context.IssueCount,
            context.Stopwatch.Elapsed);
    }

    private static void ScanDirectory(
        string path,
        ScanNode node,
        ScanAccumulator accumulator,
        ScanContext context,
        bool parallelChildren)
    {
        context.CancellationToken.ThrowIfCancellationRequested();
        context.CountFolder();

        var subDirectories = new List<ScanNode>();
        long smallSize = 0;
        long smallCount = 0;
        long filesSeen = 0;
        long bytesSeen = 0;

        try
        {
            var entries = new FileSystemEnumerable<EntryData>(
                path,
                static (ref FileSystemEntry entry) => new EntryData(
                    entry.FileName.ToString(),
                    entry.IsDirectory,
                    entry.Length,
                    (entry.Attributes & FileAttributes.ReparsePoint) != 0),
                EnumOptions);

            foreach (var entry in entries)
            {
                if (entry.IsDirectory)
                {
                    if (entry.IsReparsePoint)
                    {
                        context.AddIssue(
                            Path.Combine(path, entry.Name),
                            ScanIssueKind.SkippedLink,
                            string.Empty);
                        continue;
                    }

                    subDirectories.Add(new ScanNode(entry.Name, true, node));
                    continue;
                }

                filesSeen++;
                bytesSeen += entry.Length;

                var extension = Path.GetExtension(entry.Name);
                accumulator.Add(extension, entry.Length);

                if (entry.Length >= context.Options.MinFileNodeSize)
                {
                    node.AddChild(new ScanNode(entry.Name, false, node, entry.Length)
                    {
                        Category = FileCategoryClassifier.Classify(extension)
                    });
                }
                else
                {
                    smallSize += entry.Length;
                    smallCount++;
                }
            }
        }
        catch (UnauthorizedAccessException ex)
        {
            context.AddIssue(path, ScanIssueKind.AccessDenied, ex.Message);
        }
        catch (IOException ex)
        {
            context.AddIssue(path, ScanIssueKind.IoError, ex.Message);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            context.AddIssue(path, ScanIssueKind.IoError, ex.Message);
        }

        if (smallCount > 0)
        {
            node.AddChild(new ScanNode(string.Empty, false, node, smallSize)
            {
                IsSmallFilesGroup = true,
                FileCount = smallCount
            });
        }

        context.AddFiles(filesSeen, bytesSeen, path);

        foreach (var child in subDirectories)
        {
            node.AddChild(child);
        }

        if (parallelChildren && subDirectories.Count > 1)
        {
            var parallelOptions = new ParallelOptions
            {
                CancellationToken = context.CancellationToken,
                MaxDegreeOfParallelism = context.Options.MaxDegreeOfParallelism
            };

            Parallel.ForEach(subDirectories, parallelOptions, child =>
            {
                var local = new ScanAccumulator();

                ScanDirectory(Path.Combine(path, child.Name), child, local, context, parallelChildren: false);

                lock (accumulator)
                {
                    accumulator.Merge(local);
                }
            });
        }
        else
        {
            foreach (var child in subDirectories)
            {
                ScanDirectory(Path.Combine(path, child.Name), child, accumulator, context, parallelChildren: false);
            }
        }

        node.Complete();
    }

    private readonly record struct EntryData(string Name, bool IsDirectory, long Length, bool IsReparsePoint);

    private sealed class ExtensionTotals
    {
        public long Size;
        public long Count;
    }

    private sealed class ScanAccumulator
    {
        public Dictionary<string, ExtensionTotals> Extensions { get; } = new(StringComparer.OrdinalIgnoreCase);

        public void Add(string extension, long size)
        {
            if (!Extensions.TryGetValue(extension, out var totals))
            {
                totals = new ExtensionTotals();
                Extensions[extension] = totals;
            }

            totals.Size += size;
            totals.Count++;
        }

        public void Merge(ScanAccumulator other)
        {
            foreach (var (extension, totals) in other.Extensions)
            {
                if (!Extensions.TryGetValue(extension, out var mine))
                {
                    mine = new ExtensionTotals();
                    Extensions[extension] = mine;
                }

                mine.Size += totals.Size;
                mine.Count += totals.Count;
            }
        }
    }

    private sealed class ScanContext
    {
        private readonly IProgress<ScanProgress>? _progress;
        private readonly ConcurrentQueue<ScanIssue> _issues = new();

        private long _files;
        private long _folders;
        private long _bytes;
        private long _lastReport;
        private long _issueCount;

        public ScanContext(ScanOptions options, IProgress<ScanProgress>? progress, CancellationToken cancellationToken)
        {
            Options = options;
            CancellationToken = cancellationToken;
            _progress = progress;
            Stopwatch = Stopwatch.StartNew();
            _lastReport = Stopwatch.GetTimestamp();
        }

        public ScanOptions Options { get; }

        public CancellationToken CancellationToken { get; }

        public Stopwatch Stopwatch { get; }

        public long IssueCount => Interlocked.Read(ref _issueCount);

        public IReadOnlyList<ScanIssue> Issues => _issues.ToArray();

        public void CountFolder() => Interlocked.Increment(ref _folders);

        public void AddFiles(long files, long bytes, string currentPath)
        {
            Interlocked.Add(ref _files, files);
            Interlocked.Add(ref _bytes, bytes);

            ReportIfDue(currentPath);
        }

        public void AddIssue(string path, ScanIssueKind kind, string message)
        {
            Interlocked.Increment(ref _issueCount);

            if (_issues.Count < MaxIssuesKept)
            {
                _issues.Enqueue(new ScanIssue(path, kind, message));
            }
        }

        // İlerleme saniyede en fazla ~8 kez bildirilir; arayüz kuyruğu şişmesin.
        private void ReportIfDue(string currentPath)
        {
            if (_progress is null)
            {
                return;
            }

            var now = Stopwatch.GetTimestamp();
            var last = Interlocked.Read(ref _lastReport);

            if (now - last < Stopwatch.Frequency / 8)
            {
                return;
            }

            if (Interlocked.CompareExchange(ref _lastReport, now, last) != last)
            {
                return;
            }

            _progress.Report(new ScanProgress(
                Interlocked.Read(ref _files),
                Interlocked.Read(ref _folders),
                Interlocked.Read(ref _bytes),
                Stopwatch.Elapsed,
                currentPath));
        }
    }
}
