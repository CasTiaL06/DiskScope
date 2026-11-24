using System.Windows;
using System.Windows.Controls;
using DiskScope.App.Helpers;
using DiskScope.App.Localization;
using DiskScope.Core.Formatting;
using DiskScope.Scanning;
using Microsoft.Win32;
using Serilog;

namespace DiskScope.App.Views;

public partial class OverviewView : UserControl
{
    private readonly DiskScanner _scanner = new();

    private IReadOnlyList<DiskInfo> _disks = Array.Empty<DiskInfo>();
    private bool _isLoading;
    private bool _loadedOnce;

    public OverviewView()
    {
        InitializeComponent();

        LocalizationManager.Instance.PropertyChanged += (_, _) => UpdateSummary();

        Loaded += async (_, _) =>
        {
            if (_loadedOnce)
            {
                return;
            }

            _loadedOnce = true;
            await LoadDisksAsync();
        };
    }

    /// <summary>Kullanıcı bir disk veya klasör için analiz başlatmak istediğinde tetiklenir.</summary>
    public event EventHandler<AnalyzeRequestedEventArgs>? AnalyzeRequested;

    private async Task LoadDisksAsync()
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
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Disk listesi okunamadı.");
        }
        finally
        {
            _isLoading = false;
            RefreshButton.IsEnabled = true;
            UpdateSummary();
        }
    }

    private void UpdateSummary()
    {
        if (_disks.Count == 0)
        {
            SummaryText.Text = string.Empty;
            TotalValue.Text = "-";
            UsedValue.Text = "-";
            FreeValue.Text = "-";
            return;
        }

        var total = _disks.Sum(disk => disk.TotalSize);
        var free = _disks.Sum(disk => disk.AvailableFreeSpace);

        SummaryText.Text = Loc.F("OverviewDriveCount", _disks.Count);
        TotalValue.Text = SizeFormatter.Format(total);
        UsedValue.Text = SizeFormatter.Format(total - free);
        FreeValue.Text = SizeFormatter.Format(free);
    }

    private async void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        await LoadDisksAsync();
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
}
