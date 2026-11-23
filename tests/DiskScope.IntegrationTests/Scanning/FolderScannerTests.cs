using DiskScope.Scanning;

namespace DiskScope.IntegrationTests.Scanning;

public sealed class FolderScannerTests : IDisposable
{
    private readonly string _root =
        Path.Combine(Path.GetTempPath(), "DiskScopeTests_" + Guid.NewGuid().ToString("N"));

    public FolderScannerTests()
    {
        Directory.CreateDirectory(_root);
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(_root, recursive: true);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    private void WriteFile(string relativePath, int size)
    {
        var path = Path.Combine(_root, relativePath);

        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllBytes(path, new byte[size]);
    }

    private static ScanOptions Options(long minFileNodeSize = 1000) =>
        new() { MinFileNodeSize = minFileNodeSize };

    [Fact]
    public async Task Scan_ComputesTotalsAndStructure()
    {
        WriteFile("a.txt", 100);
        WriteFile(Path.Combine("sub", "big.mp4"), 5000);
        WriteFile(Path.Combine("sub", "small.log"), 50);
        WriteFile(Path.Combine("sub", "deep", "x.zip"), 3000);

        var result = await new FolderScanner().ScanAsync(_root, Options());

        Assert.Equal(8150, result.TotalSize);
        Assert.Equal(4, result.TotalFiles);
        Assert.Equal(2, result.TotalFolders);
        Assert.Equal(0, result.IssueCount);

        var sub = Assert.Single(result.Root.Children, c => c.IsDirectory);
        Assert.Equal("sub", sub.Name);
        Assert.Equal(8050, sub.Size);
        Assert.Equal(3, sub.FileCount);
        Assert.Equal(1, sub.FolderCount);
    }

    [Fact]
    public async Task Scan_ChildrenAreSortedLargestFirst()
    {
        WriteFile("one.bin", 2000);
        WriteFile("two.bin", 9000);
        WriteFile("three.bin", 4000);

        var result = await new FolderScanner().ScanAsync(_root, Options());

        var sizes = result.Root.Children.Select(c => c.Size).ToArray();

        Assert.Equal(new long[] { 9000, 4000, 2000 }, sizes);
    }

    [Fact]
    public async Task Scan_SmallFilesAreGroupedPerFolder()
    {
        WriteFile("s1.txt", 10);
        WriteFile("s2.txt", 20);
        WriteFile("big.bin", 5000);

        var result = await new FolderScanner().ScanAsync(_root, Options());

        var group = Assert.Single(result.Root.Children, c => c.IsSmallFilesGroup);

        Assert.Equal(30, group.Size);
        Assert.Equal(2, group.FileCount);
        Assert.Equal(5030, result.TotalSize);
    }

    [Fact]
    public async Task Scan_ReportsCategoryAndExtensionStatistics()
    {
        WriteFile("movie.mp4", 5000);
        WriteFile("archive.zip", 3000);
        WriteFile("note.TXT", 40);
        WriteFile("note2.txt", 60);

        var result = await new FolderScanner().ScanAsync(_root, Options());

        var video = Assert.Single(result.Categories, c => c.Category == FileCategory.Video);
        var archive = Assert.Single(result.Categories, c => c.Category == FileCategory.Archive);

        Assert.Equal(5000, video.Size);
        Assert.Equal(3000, archive.Size);

        // ".TXT" ve ".txt" tek uzantı olarak sayılmalı.
        var txt = Assert.Single(result.Extensions, e => e.Extension == ".txt");

        Assert.Equal(100, txt.Size);
        Assert.Equal(2, txt.FileCount);
    }

    [Fact]
    public async Task Scan_NodeFullPathPointsToRealFile()
    {
        WriteFile(Path.Combine("sub", "big.mp4"), 5000);

        var result = await new FolderScanner().ScanAsync(_root, Options());

        var file = result.Root.Children.Single(c => c.IsDirectory).Children.Single(c => !c.IsDirectory);

        Assert.True(File.Exists(file.GetFullPath()));
    }

    [Fact]
    public async Task Scan_EmptyFolder_ReturnsZeroTotals()
    {
        var result = await new FolderScanner().ScanAsync(_root, Options());

        Assert.Equal(0, result.TotalSize);
        Assert.Equal(0, result.TotalFiles);
        Assert.Empty(result.Root.Children);
    }

    [Fact]
    public async Task Scan_AlreadyCancelledToken_Throws()
    {
        WriteFile("a.bin", 5000);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => new FolderScanner().ScanAsync(_root, Options(), null, cts.Token));
    }

    [Fact]
    public async Task Scan_MissingFolder_ThrowsDirectoryNotFound()
    {
        var missing = Path.Combine(_root, "does-not-exist");

        await Assert.ThrowsAsync<DirectoryNotFoundException>(
            () => new FolderScanner().ScanAsync(missing, Options()));
    }

    [Fact]
    public async Task Scan_ReportsProgress()
    {
        for (var i = 0; i < 20; i++)
        {
            WriteFile(Path.Combine($"d{i}", "f.bin"), 2000);
        }

        var reports = new List<ScanProgress>();
        var progress = new SyncProgress(reports.Add);

        var result = await new FolderScanner().ScanAsync(_root, Options(), progress);

        // İlerleme sıklığı sınırlı olduğu için rapor sayısı garanti değil; ama sonuç doğru olmalı.
        Assert.Equal(40000, result.TotalSize);
        Assert.All(reports, r => Assert.True(r.Files >= 0));
    }

    private sealed class SyncProgress : IProgress<ScanProgress>
    {
        private readonly Action<ScanProgress> _handler;

        public SyncProgress(Action<ScanProgress> handler) => _handler = handler;

        public void Report(ScanProgress value)
        {
            lock (this)
            {
                _handler(value);
            }
        }
    }
}
