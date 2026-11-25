using System.Windows;
using System.Windows.Controls;
using DiskScope.App.Helpers;
using DiskScope.App.Localization;
using DiskScope.App.Services;
using DiskScope.Core.Formatting;
using DiskScope.Core.History;
using Serilog;

namespace DiskScope.App.Views;

public partial class HistoryView : UserControl
{
    private List<ScanSummary> _scans = new();

    public HistoryView()
    {
        InitializeComponent();

        AppServices.ScanHistoryChanged += async (_, _) => await LoadAsync();
        LocalizationManager.Instance.PropertyChanged += (_, _) => ShowRows();

        Loaded += async (_, _) => await LoadAsync();
    }

    /// <summary>"Tekrar analiz et" düğmesine basıldı.</summary>
    public event EventHandler<AnalyzeRequestedEventArgs>? AnalyzeRequested;

    private async Task LoadAsync()
    {
        try
        {
            _scans = await AppServices.LoadScansAsync();
            ShowRows();
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Tarama geçmişi okunamadı.");
        }
    }

    private void ShowRows()
    {
        HistoryList.ItemsSource = _scans
            .Select(scan => new ScanRow(
                scan.Path,
                scan.Path,
                DateText.Describe(scan.ScannedAtUtc),
                SizeFormatter.Format(scan.TotalSize),
                Loc.F(
                    "HistoryDetail",
                    scan.TotalFiles,
                    scan.TotalFolders,
                    TimeSpan.FromSeconds(scan.ElapsedSeconds).ToString(scan.ElapsedSeconds >= 3600 ? @"h\:mm\:ss" : @"mm\:ss"))))
            .ToList();

        EmptyText.Visibility = _scans.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        ClearButton.IsEnabled = _scans.Count > 0;
    }

    private void Reanalyze_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: ScanRow row })
        {
            AnalyzeRequested?.Invoke(
                this,
                new AnalyzeRequestedEventArgs(row.Path, DriveHelper.GetUsedBytesIfRoot(row.Path)));
        }
    }

    private async void ClearButton_Click(object sender, RoutedEventArgs e)
    {
        var answer = MessageBox.Show(
            Loc.T("HistoryClearConfirm"),
            Loc.T("HistoryClear"),
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (answer != MessageBoxResult.Yes)
        {
            return;
        }

        try
        {
            await AppServices.ClearScansAsync();
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Tarama geçmişi temizlenemedi.");
        }
    }
}
