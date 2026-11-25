using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using DiskScope.App.Helpers;
using DiskScope.App.Views;
using Serilog;

namespace DiskScope.App;

public partial class MainWindow : Window
{
    private readonly Dictionary<RadioButton, FrameworkElement> _pages;

    public MainWindow()
    {
        InitializeComponent();

        _pages = new Dictionary<RadioButton, FrameworkElement>
        {
            [NavOverview] = Overview,
            [NavAnalyzer] = Analyzer,
            [NavHealth] = Health,
            [NavHistory] = History,
            [NavSettings] = Settings,
            [NavAbout] = About
        };

        SourceInitialized += (_, _) => DarkTitleBar.Apply(this);

        Overview.AnalyzeRequested += async (_, e) => await AnalyzeAsync(e.Path, e.ExpectedBytes);
        History.AnalyzeRequested += async (_, e) => await AnalyzeAsync(e.Path, e.ExpectedBytes);
        Overview.NavigateRequested += (_, page) => Navigate(page);
        Analyzer.ChooseDiskRequested += (_, _) => Navigate("Overview");

        // Olay bağlantıları hazır olduktan sonra ilk sayfayı seç.
        NavOverview.IsChecked = true;
    }

    private void Navigate(string page)
    {
        var target = page switch
        {
            "Analyzer" => NavAnalyzer,
            "Health" => NavHealth,
            "History" => NavHistory,
            "Settings" => NavSettings,
            "About" => NavAbout,
            _ => NavOverview
        };

        target.IsChecked = true;
    }

    private async Task AnalyzeAsync(string path, long? expectedBytes)
    {
        try
        {
            Navigate("Analyzer");

            await Analyzer.StartScanAsync(path, expectedBytes);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Analiz başlatılamadı: {Path}", path);
        }
    }

    private void Nav_Checked(object sender, RoutedEventArgs e)
    {
        foreach (var (button, page) in _pages)
        {
            Show(page, button.IsChecked == true);
        }
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
}
