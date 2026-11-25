using DiskScope.Core.History;
using DiskScope.Scanning;
using DiskScope.Storage.History;
using DiskScope.Storage.Settings;

namespace DiskScope.App.Services;

/// <summary>Uygulama genelinde paylaşılan hizmetler (ayarlar, geçmiş kayıtları, arka plan, disk sağlığı).</summary>
internal static class AppServices
{
    private static readonly SemaphoreSlim HistoryLock = new(1, 1);

    public static string DataDirectory { get; private set; } = string.Empty;

    public static AppSettings Settings { get; private set; } = new();

    public static JsonSettingsService SettingsService { get; private set; } = null!;

    public static JsonListStore<ScanSummary> ScanStore { get; private set; } = null!;

    public static JsonListStore<UsageSnapshot> UsageStore { get; private set; } = null!;

    public static BackgroundService Background { get; private set; } = null!;

    public static DiskHealthService Health { get; } = new();

    /// <summary>Tarama geçmişi değiştiğinde (yeni tarama eklendi veya geçmiş temizlendi) tetiklenir.</summary>
    public static event EventHandler? ScanHistoryChanged;

    public static void Initialize(string dataDirectory, JsonSettingsService settingsService, AppSettings settings)
    {
        DataDirectory = dataDirectory;
        SettingsService = settingsService;
        Settings = settings;

        ScanStore = new JsonListStore<ScanSummary>(dataDirectory, "scan-history.json");
        UsageStore = new JsonListStore<UsageSnapshot>(dataDirectory, "usage-history.json");

        Background = new BackgroundService(dataDirectory);
        Background.Apply(settings);
    }

    public static Task SaveSettingsAsync() => SettingsService.SaveAsync(Settings);

    public static Task<List<ScanSummary>> LoadScansAsync() => ScanStore.LoadAsync();

    public static async Task AddScanAsync(ScanSummary summary)
    {
        await HistoryLock.WaitAsync();

        try
        {
            var list = await ScanStore.LoadAsync();

            HistoryLogic.AddScan(list, summary);

            await ScanStore.SaveAsync(list);
        }
        finally
        {
            HistoryLock.Release();
        }

        ScanHistoryChanged?.Invoke(null, EventArgs.Empty);
    }

    public static async Task ClearScansAsync()
    {
        await HistoryLock.WaitAsync();

        try
        {
            await ScanStore.SaveAsync(new List<ScanSummary>());
        }
        finally
        {
            HistoryLock.Release();
        }

        ScanHistoryChanged?.Invoke(null, EventArgs.Empty);
    }

    /// <summary>Bugünün doluluk kaydını yazar ve kayıtlı tüm günleri döndürür.</summary>
    public static async Task<List<UsageSnapshot>> RecordUsageAsync(long usedBytes, long totalBytes)
    {
        await HistoryLock.WaitAsync();

        try
        {
            var list = await UsageStore.LoadAsync();

            HistoryLogic.RecordUsage(list, new UsageSnapshot
            {
                Day = DateTime.Now.ToString("yyyy-MM-dd"),
                UsedBytes = usedBytes,
                TotalBytes = totalBytes
            });

            await UsageStore.SaveAsync(list);

            return list;
        }
        finally
        {
            HistoryLock.Release();
        }
    }
}
