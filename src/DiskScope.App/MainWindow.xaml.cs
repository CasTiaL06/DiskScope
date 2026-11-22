using System.Globalization;
using System.Windows;
using DiskScope.Scanning;
using Serilog;

namespace DiskScope.App;

public partial class MainWindow : Window
{
    private readonly DiskScanner _scanner = new();

    private bool _isLoadingDisks;

    public MainWindow()
    {
        InitializeComponent();

        Loaded += async (_, _) => await LoadDisksAsync();
    }

    private async Task LoadDisksAsync()
    {
        // Aynı anda iki yenileme başlamasın.
        if (_isLoadingDisks)
        {
            return;
        }

        _isLoadingDisks = true;
        RefreshButton.IsEnabled = false;

        try
        {
            // Disk bilgileri arka planda okunur; böylece pencere donmaz.
            var disks = await Task.Run(() => _scanner.GetDisks());

            DrivesList.ItemsSource = disks;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Disk listesi okunamadı.");
        }
        finally
        {
            _isLoadingDisks = false;
            RefreshButton.IsEnabled = true;
        }
    }

    private async void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        await LoadDisksAsync();
    }

    private async void LanguageToggle_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var app = (App)Application.Current;

            var culture = app.Localization.CurrentCulture.Name == "tr-TR"
                ? CultureInfo.GetCultureInfo("en-US")
                : CultureInfo.GetCultureInfo("tr-TR");

            await app.SetLanguageAsync(culture);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Dil değiştirilemedi.");
        }
    }
}
