using System.Globalization;
using System.Windows;
using DiskScope.App.Helpers;
using DiskScope.App.Views;
using Serilog;

namespace DiskScope.App;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        SourceInitialized += (_, _) => DarkTitleBar.Apply(this);

        Overview.AnalyzeRequested += async (_, e) => await AnalyzeAsync(e.Path, e.ExpectedBytes);
        Analyzer.ChooseDiskRequested += (_, _) => NavOverview.IsChecked = true;

        // Olay bağlantıları hazır olduktan sonra ilk sayfayı seç.
        NavOverview.IsChecked = true;
    }

    private async Task AnalyzeAsync(string path, long? expectedBytes)
    {
        try
        {
            NavAnalyzer.IsChecked = true;

            await Analyzer.StartScanAsync(path, expectedBytes);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Analiz başlatılamadı: {Path}", path);
        }
    }

    private void Nav_Checked(object sender, RoutedEventArgs e)
    {
        Overview.Visibility = NavOverview.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
        Analyzer.Visibility = NavAnalyzer.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
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
