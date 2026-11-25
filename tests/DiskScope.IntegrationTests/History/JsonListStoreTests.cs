using DiskScope.Core.History;
using DiskScope.Storage.History;

namespace DiskScope.IntegrationTests.History;

public sealed class JsonListStoreTests : IDisposable
{
    private readonly string _directory =
        Path.Combine(Path.GetTempPath(), "NexDiskTests_" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        try
        {
            Directory.Delete(_directory, recursive: true);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    [Fact]
    public async Task Load_WhenFileMissing_ReturnsEmptyList()
    {
        var store = new JsonListStore<ScanSummary>(_directory, "scans.json");

        Assert.Empty(await store.LoadAsync());
    }

    [Fact]
    public async Task SaveThenLoad_RoundTripsData()
    {
        var store = new JsonListStore<ScanSummary>(_directory, "scans.json");

        var summary = new ScanSummary
        {
            Path = @"D:\",
            ScannedAtUtc = new DateTime(2026, 11, 24, 10, 30, 0, DateTimeKind.Utc),
            TotalSize = 1234,
            TotalFiles = 10,
            TopFolders = { new FolderUsage { Name = "Games", Size = 1000 } },
            Categories = { new CategoryUsage { Category = "Video", Size = 500, FileCount = 2 } }
        };

        await store.SaveAsync(new[] { summary });

        var loaded = Assert.Single(await store.LoadAsync());

        Assert.Equal(@"D:\", loaded.Path);
        Assert.Equal(1234, loaded.TotalSize);
        Assert.Equal("Games", Assert.Single(loaded.TopFolders).Name);
        Assert.Equal("Video", Assert.Single(loaded.Categories).Category);
    }

    [Fact]
    public async Task Load_WhenFileIsCorrupt_ReturnsEmptyListWithoutThrowing()
    {
        Directory.CreateDirectory(_directory);
        await File.WriteAllTextAsync(Path.Combine(_directory, "scans.json"), "{ bozuk json ");

        var store = new JsonListStore<ScanSummary>(_directory, "scans.json");

        Assert.Empty(await store.LoadAsync());
    }
}
