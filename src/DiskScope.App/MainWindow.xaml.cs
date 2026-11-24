using System.Globalization;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
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
        Show(Overview, NavOverview.IsChecked == true);
        Show(Analyzer, NavAnalyzer.IsChecked == true);
    }

    /// <summary>Sayfayı gösterir/gizler; yeni açılan sayfa yumuşakça belirir.</summary>
    private static void Show(FrameworkElement page, bool visible)
    {
        if (!visible)
        {
            page.Visibility = Visibility.Collapsed;
            return;
        }

        if (page.Visibility == Visibility.Visible)
        {
            return;
        }

        page.Visibility = Visibility.Visible;

        var slide = new TranslateTransform(0, 14);
        page.RenderTransform = slide;

        page.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(260)));

        slide.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(14, 0, TimeSpan.FromMilliseconds(340))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        });
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
