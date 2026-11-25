using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using DiskScope.App.Controls;
using DiskScope.App.Helpers;
using DiskScope.App.Localization;
using DiskScope.App.Services;
using DiskScope.App.Visuals;
using DiskScope.Core.Formatting;
using DiskScope.Core.History;
using DiskScope.Scanning;
using Microsoft.Win32;
using Serilog;

namespace DiskScope.App.Views;

public partial class OverviewView : UserControl
{
    private readonly DiskScanner _scanner = new();

    private IReadOnlyList<DiskInfo> _disks = Array.Empty<DiskInfo>();
    private IReadOnlyList<PhysicalDiskHealth> _health = Array.Empty<PhysicalDiskHealth>();
    private List<ScanSummary> _scans = new();
    private List<UsageSnapshot> _usage = new();

    private bool _isLoading;
    private bool _loadedOnce;

    public OverviewView()
    {
        InitializeComponent();

        ApplyHero();

        AppServices.Background.Changed += (_, _) => ApplyHero();
        AppServices.ScanHistoryChanged += async (_, _) => await LoadHistoryAsync();
        LocalizationManager.Instance.PropertyChanged += (_, _) => RefreshTexts();

        Loaded += async (_, _) =>
        {
            if (_loadedOnce)
            {
                return;
            }

            _loadedOnce = true;
            await LoadAllAsync();
        };
    }

    /// <summary>Kullanıcı bir disk veya klasör için analiz başlatmak istediğinde tetiklenir.</summary>
    public event EventHandler<AnalyzeRequestedEventArgs>? AnalyzeRequested;

    /// <summary>Başka bir sayfaya geçilmesi istendiğinde tetiklenir ("Analyzer", "Health", "History").</summary>
    public event EventHandler<string>? NavigateRequested;

    private void ApplyHero()
    {
        HeroBrush.ImageSource = AppServices.Background.Current;
    }

    // ------------------------------------------------------------------
    // Veri yükleme
    // ------------------------------------------------------------------

    private async Task LoadAllAsync()
    {
        if (_isLoading)
        {
            return;
        }

        _isLoading = true;
        RefreshButton.IsEnabled = false;

        try
        {
            // Diskler arka planda okunur; pencere donmaz.
            _disks = await Task.Run(() => _scanner.GetDisks());

            DrivesList.ItemsSource = _disks;
            EmptyText.Visibility = _disks.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

            UpdateSummary();

            var (used, total) = GetUsage();

            _usage = total > 0
                ? await AppServices.RecordUsageAsync(used, total)
                : await AppServices.UsageStore.LoadAsync();

            UpdateChart();

            await LoadHistoryAsync();

            _health = await Task.Run(() => AppServices.Health.GetAll());
            UpdateHealth();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Genel Bakış verileri yüklenemedi.");
        }
        finally
        {
            _isLoading = false;
            RefreshButton.IsEnabled = true;
        }
    }

    private async Task LoadHistoryAsync()
    {
        try
        {
            _scans = await AppServices.LoadScansAsync();
            UpdateHistoryViews();
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Tarama geçmişi okunamadı.");
        }
    }

    private void RefreshTexts()
    {
        UpdateSummary();
        UpdateChart();
        UpdateHistoryViews();
        UpdateHealth();
    }

    // ------------------------------------------------------------------
    // Bölümler
    // ------------------------------------------------------------------

    /// <summary>Sabit disklerin toplam kullanılan ve toplam boyutu (sabit disk yoksa tüm diskler).</summary>
    private (long Used, long Total) GetUsage()
    {
        var fixedDisks = _disks.Where(d => d.DriveType == nameof(DriveType.Fixed)).ToList();
        var source = fixedDisks.Count > 0 ? fixedDisks : _disks.ToList();

        var total = source.Sum(d => d.TotalSize);
        var free = source.Sum(d => d.AvailableFreeSpace);

        return (total - free, total);
    }

    private void UpdateSummary()
    {
        var (used, total) = GetUsage();

        if (total <= 0)
        {
            TotalValue.Text = "—";
            UsedValue.Text = "—";
            FreeValue.Text = "—";
            UsedPercentText.Text = string.Empty;
            FreePercentText.Text = string.Empty;
            UsedBar.Value = 0;
            FreeBar.Value = 0;
            OverallRing.Value = 0;
            OverallPercentText.Text = string.Empty;
            return;
        }

        var usedPercent = used * 100.0 / total;

        TotalValue.Text = SizeFormatter.Format(total);
        UsedValue.Text = SizeFormatter.Format(used);
        FreeValue.Text = SizeFormatter.Format(total - used);

        UsedPercentText.Text = Loc.F("PercentFormat", usedPercent);
        FreePercentText.Text = Loc.F("PercentFormat", 100 - usedPercent);

        UsedBar.Value = usedPercent;
        FreeBar.Value = 100 - usedPercent;

        OverallRing.Value = usedPercent;
        OverallPercentText.Text = Loc.F("PercentWhole", usedPercent);
    }

    private void UpdateChart()
    {
        var recent = HistoryLogic.LastDays(_usage, 7, DateTime.Now);

        Chart.EmptyMessage = Loc.T("ChartCollecting");
        Chart.Points = recent
            .Select(s => new ChartPoint(DayLabel(s.Day), s.UsedPercent))
            .ToList();
    }

    private static string DayLabel(string day) =>
        DateTime.TryParseExact(day, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed)
            ? parsed.ToString("ddd", CultureInfo.CurrentCulture)
            : day;

    private void UpdateHistoryViews()
    {
        var latest = _scans.FirstOrDefault();

        // Kategori dağılımı (son taramadan)
        if (latest is null || latest.Categories.Count == 0)
        {
            LegendList.ItemsSource = null;
            LegendEmptyText.Visibility = Visibility.Visible;
            LegendSourceText.Text = string.Empty;
        }
        else
        {
            LegendList.ItemsSource = latest.Categories
                .OrderByDescending(c => c.Size)
                .Take(6)
                .Select(c =>
                {
                    var category = Enum.TryParse<FileCategory>(c.Category, out var parsed) ? parsed : FileCategory.Other;
                    var percent = latest.TotalSize > 0 ? c.Size * 100.0 / latest.TotalSize : 0;

                    return new LegendRow(
                        CategoryPalette.GetBrush(category),
                        CategoryPalette.GetName(category),
                        SizeFormatter.Format(c.Size),
                        Loc.F("PercentFormat", percent));
                })
                .ToList();

            LegendEmptyText.Visibility = Visibility.Collapsed;
            LegendSourceText.Text = Loc.F("LegendSource", latest.Path, DateText.Describe(latest.ScannedAtUtc));
        }

        // En çok yer kaplayan klasörler (son taramadan)
        if (latest is null || latest.TopFolders.Count == 0)
        {
            TopFoldersList.ItemsSource = null;
            TopFoldersEmpty.Visibility = Visibility.Visible;
        }
        else
        {
            var largest = latest.TopFolders.Max(f => f.Size);

            TopFoldersList.ItemsSource = latest.TopFolders
                .Select(f => new TopFolderRow(f.Name, SizeFormatter.Format(f.Size), largest > 0 ? f.Size * 100.0 / largest : 0))
                .ToList();

            TopFoldersEmpty.Visibility = Visibility.Collapsed;
        }

        // Son analizler
        RecentList.ItemsSource = _scans
            .Take(3)
            .Select(s => new ScanRow(
                s.Path,
                s.Path,
                DateText.Describe(s.ScannedAtUtc),
                SizeFormatter.Format(s.TotalSize),
                string.Empty))
            .ToList();

        RecentEmpty.Visibility = _scans.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void UpdateHealth()
    {
        PhysicalDiskHealth? health = null;

        // Önce Windows'un kurulu olduğu diskin sağlığı gösterilir.
        var systemRoot = Path.GetPathRoot(Environment.SystemDirectory);
        var systemDisk = _disks.FirstOrDefault(d =>
            string.Equals(d.DriveLetter, systemRoot, StringComparison.OrdinalIgnoreCase));

        if (systemDisk?.DiskNumber is { } diskNumber)
        {
            health = _health.FirstOrDefault(h => h.DiskNumber == diskNumber);
        }

        health ??= _health.FirstOrDefault();

        var status = health?.Status ?? DiskHealthStatus.Unknown;

        HealthGlyph.Text = HealthStyle.Glyph(status);
        HealthGlyph.Foreground = HealthStyle.Foreground(status);
        HealthChip.Background = HealthStyle.Background(status);

        HealthStatusText.Text = HealthStyle.Text(status);
        HealthStatusText.Foreground = HealthStyle.Foreground(status);

        HealthDetailText.Text = health is null ? Loc.T("HealthUnavailable") : health.Name;

        HealthTempValue.Text = health?.TemperatureCelsius is { } temperature
            ? Loc.F("TemperatureFormat", temperature)
            : "—";

        HealthHoursValue.Text = health?.PowerOnHours is { } hours
            ? Loc.F("HoursFormat", hours)
            : "—";

        var hint = health is null || (health.TemperatureCelsius is null && health.PowerOnHours is null)
            ? Loc.T("HealthNeedsAdmin")
            : null;

        HealthTempValue.ToolTip = hint;
        HealthHoursValue.ToolTip = hint;
    }

    // ------------------------------------------------------------------
    // Olaylar
    // ------------------------------------------------------------------

    private async void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        await LoadAllAsync();
    }

    private void AnalyzeButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: DiskInfo disk })
        {
            AnalyzeRequested?.Invoke(
                this,
                new AnalyzeRequestedEventArgs(disk.DriveLetter, disk.UsedSpace));
        }
    }

    private void PickFolder_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog { Multiselect = false };

        if (dialog.ShowDialog(Window.GetWindow(this)) == true)
        {
            AnalyzeRequested?.Invoke(
                this,
                new AnalyzeRequestedEventArgs(
                    dialog.FolderName,
                    DriveHelper.GetUsedBytesIfRoot(dialog.FolderName)));
        }
    }

    private void RecentScan_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: ScanRow row })
        {
            AnalyzeRequested?.Invoke(
                this,
                new AnalyzeRequestedEventArgs(row.Path, DriveHelper.GetUsedBytesIfRoot(row.Path)));
        }
    }

    private void SeeAnalyzer_Click(object sender, RoutedEventArgs e) => NavigateRequested?.Invoke(this, "Analyzer");

    private void SeeHealth_Click(object sender, RoutedEventArgs e) => NavigateRequested?.Invoke(this, "Health");

    private void SeeHistory_Click(object sender, RoutedEventArgs e) => NavigateRequested?.Invoke(this, "History");
}
