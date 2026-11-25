namespace DiskScope.Core.History;

/// <summary>Geçmiş listelerini güncelleyen saf (dosya/arayüz kullanmayan) mantık.</summary>
public static class HistoryLogic
{
    public const int MaxScans = 50;
    public const int MaxUsageDays = 60;

    /// <summary>Yeni taramayı listenin başına ekler; en fazla <paramref name="max"/> kayıt tutar.</summary>
    public static void AddScan(List<ScanSummary> list, ScanSummary summary, int max = MaxScans)
    {
        ArgumentNullException.ThrowIfNull(list);
        ArgumentNullException.ThrowIfNull(summary);

        list.Insert(0, summary);

        if (list.Count > max)
        {
            list.RemoveRange(max, list.Count - max);
        }
    }

    /// <summary>
    /// Günlük doluluk kaydı ekler. Aynı güne ait kayıt varsa güncellenir; liste tarihe göre sıralı kalır
    /// ve en fazla <paramref name="maxDays"/> gün tutulur.
    /// </summary>
    public static void RecordUsage(List<UsageSnapshot> list, UsageSnapshot snapshot, int maxDays = MaxUsageDays)
    {
        ArgumentNullException.ThrowIfNull(list);
        ArgumentNullException.ThrowIfNull(snapshot);

        list.RemoveAll(existing => existing.Day == snapshot.Day);
        list.Add(snapshot);
        list.Sort((a, b) => string.CompareOrdinal(a.Day, b.Day));

        if (list.Count > maxDays)
        {
            list.RemoveRange(0, list.Count - maxDays);
        }
    }

    /// <summary>Bugünden geriye doğru <paramref name="days"/> gün içindeki kayıtları (eskiden yeniye) döndürür.</summary>
    public static List<UsageSnapshot> LastDays(IEnumerable<UsageSnapshot> snapshots, int days, DateTime today)
    {
        ArgumentNullException.ThrowIfNull(snapshots);

        var oldest = today.Date.AddDays(-(days - 1)).ToString("yyyy-MM-dd");

        return snapshots
            .Where(s => string.CompareOrdinal(s.Day, oldest) >= 0)
            .OrderBy(s => s.Day, StringComparer.Ordinal)
            .ToList();
    }
}
