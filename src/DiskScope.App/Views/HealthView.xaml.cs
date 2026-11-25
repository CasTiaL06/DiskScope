using System.Windows;
using System.Windows.Controls;
using DiskScope.App.Localization;
using DiskScope.App.Services;
using DiskScope.Core.Formatting;
using DiskScope.Scanning;
using Serilog;

namespace DiskScope.App.Views;

public partial class HealthView : UserControl
{
    private IReadOnlyList<PhysicalDiskHealth> _disks = Array.Empty<PhysicalDiskHealth>();
    private bool _isLoading;
    private bool _loadedOnce;

    public HealthView()
    {
        InitializeComponent();

        LocalizationManager.Instance.PropertyChanged += (_, _) => ShowRows();

        Loaded += async (_, _) =>
        {
            if (_loadedOnce)
            {
                return;
            }

            _loadedOnce = true;
            await LoadAsync();
        };
    }

    private async Task LoadAsync()
    {
        if (_isLoading)
        {
            return;
        }

        _isLoading = true;
        RefreshButton.IsEnabled = false;

        try
        {
            _disks = await Task.Run(() => AppServices.Health.GetAll());
            ShowRows();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Disk sağlığı okunamadı.");
        }
        finally
        {
            _isLoading = false;
            RefreshButton.IsEnabled = true;
        }
    }

    private void ShowRows()
    {
        HealthList.ItemsSource = _disks
            .Select(disk => new HealthRow(
                disk.Name,
                disk.MediaType switch
                {
                    DiskMediaType.Hdd => Loc.T("DiskMediaHdd"),
                    DiskMediaType.Ssd => Loc.T("DiskMediaSsd"),
                    DiskMediaType.Nvme => Loc.T("DiskMediaNvme"),
                    _ => Loc.T("DiskMediaUnknown")
                },
                HealthStyle.Text(disk.Status),
                HealthStyle.Foreground(disk.Status),
                HealthStyle.Background(disk.Status),
                HealthStyle.Glyph(disk.Status),
                disk.TemperatureCelsius is { } temperature ? Loc.F("TemperatureFormat", temperature) : "—",
                disk.PowerOnHours is { } hours ? Loc.F("HoursFormat", hours) : "—",
                SizeFormatter.Format(disk.SizeBytes)))
            .ToList();

        EmptyText.Visibility = _disks.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private async void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        await LoadAsync();
    }
}
