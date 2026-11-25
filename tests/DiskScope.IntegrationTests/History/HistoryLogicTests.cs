using DiskScope.Core.History;

namespace DiskScope.IntegrationTests.History;

public sealed class HistoryLogicTests
{
    [Fact]
    public void AddScan_InsertsNewestFirstAndTrims()
    {
        var list = new List<ScanSummary>();

        for (var i = 0; i < 5; i++)
        {
            HistoryLogic.AddScan(list, new ScanSummary { Path = $"D{i}" }, max: 3);
        }

        Assert.Equal(3, list.Count);
        Assert.Equal("D4", list[0].Path);
        Assert.Equal("D2", list[2].Path);
    }

    [Fact]
    public void RecordUsage_ReplacesSameDayAndKeepsOrder()
    {
        var list = new List<UsageSnapshot>();

        HistoryLogic.RecordUsage(list, new UsageSnapshot { Day = "2026-11-22", UsedBytes = 10, TotalBytes = 100 });
        HistoryLogic.RecordUsage(list, new UsageSnapshot { Day = "2026-11-20", UsedBytes = 5, TotalBytes = 100 });
        HistoryLogic.RecordUsage(list, new UsageSnapshot { Day = "2026-11-22", UsedBytes = 20, TotalBytes = 100 });

        Assert.Equal(2, list.Count);
        Assert.Equal("2026-11-20", list[0].Day);
        Assert.Equal(20, list[1].UsedPercent, 6);
    }

    [Fact]
    public void RecordUsage_KeepsOnlyMaxDays()
    {
        var list = new List<UsageSnapshot>();

        for (var i = 1; i <= 10; i++)
        {
            HistoryLogic.RecordUsage(list, new UsageSnapshot { Day = $"2026-11-{i:00}", TotalBytes = 1 }, maxDays: 4);
        }

        Assert.Equal(4, list.Count);
        Assert.Equal("2026-11-07", list[0].Day);
        Assert.Equal("2026-11-10", list[3].Day);
    }

    [Fact]
    public void LastDays_ReturnsOnlyRecentSnapshotsInOrder()
    {
        var snapshots = new[]
        {
            new UsageSnapshot { Day = "2026-11-10", TotalBytes = 1 },
            new UsageSnapshot { Day = "2026-11-20", TotalBytes = 1 },
            new UsageSnapshot { Day = "2026-11-24", TotalBytes = 1 },
            new UsageSnapshot { Day = "2026-11-22", TotalBytes = 1 }
        };

        var result = HistoryLogic.LastDays(snapshots, 7, new DateTime(2026, 11, 24));

        Assert.Equal(new[] { "2026-11-20", "2026-11-22", "2026-11-24" }, result.Select(s => s.Day));
    }

    [Fact]
    public void UsedPercent_IsZeroWhenTotalIsZero()
    {
        Assert.Equal(0, new UsageSnapshot { UsedBytes = 5, TotalBytes = 0 }.UsedPercent);
    }
}
