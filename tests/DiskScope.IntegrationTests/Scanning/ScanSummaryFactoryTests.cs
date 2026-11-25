using DiskScope.Scanning;

namespace DiskScope.IntegrationTests.Scanning;

public sealed class ScanSummaryFactoryTests : IDisposable
{
    private readonly string _root =
        Path.Combine(Path.GetTempPath(), "NexDiskSummary_" + Guid.NewGuid().ToString("N"));

    public ScanSummaryFactoryTests()
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

    private void WriteFile(string relative, int size)
    {
        var path = Path.Combine(_root, relative);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllBytes(path, new byte[size]);
    }

    [Fact]
    public async Task Create_ContainsTopFoldersCategoriesAndTotals()
    {
        WriteFile(Path.Combine("Big", "a.mp4"), 9000);
        WriteFile(Path.Combine("Medium", "b.zip"), 5000);
        WriteFile(Path.Combine("Small", "c.txt"), 100);

        var result = await new FolderScanner().ScanAsync(_root, new ScanOptions { MinFileNodeSize = 1000 });

        var summary = ScanSummaryFactory.Create(result, new DateTime(2026, 11, 24, 0, 0, 0, DateTimeKind.Utc), topFolderCount: 2);

        Assert.Equal(result.RootPath, summary.Path);
        Assert.Equal(14100, summary.TotalSize);
        Assert.Equal(3, summary.TotalFiles);

        Assert.Equal(new[] { "Big", "Medium" }, summary.TopFolders.Select(f => f.Name));
        Assert.Contains(summary.Categories, c => c.Category == "Video" && c.Size == 9000);
    }
}
