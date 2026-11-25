using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using DiskScope.App.Localization;
using DiskScope.App.Services;
using DiskScope.Storage.Settings;
using Microsoft.Win32;
using Serilog;

namespace DiskScope.App.Views;

public partial class SettingsView : UserControl
{
    private static readonly string[] PresetNameKeys =
    [
        "BackgroundNeonPlatter",
        "BackgroundAurora",
        "BackgroundGrid",
        "BackgroundWaves"
    ];

    private bool _initializing = true;

    public SettingsView()
    {
        InitializeComponent();

        DataPathText.Text = AppServices.DataDirectory;

        var app = (App)Application.Current;

        LangTurkish.IsChecked = app.Localization.CurrentCulture.Name == "tr-TR";
        LangEnglish.IsChecked = app.Localization.CurrentCulture.Name != "tr-TR";

        _initializing = false;

        LocalizationManager.Instance.PropertyChanged += (_, _) => RebuildBackgrounds();

        RebuildBackgrounds();
    }

    // ------------------------------------------------------------------
    // Dil
    // ------------------------------------------------------------------

    private async void Language_Checked(object sender, RoutedEventArgs e)
    {
        if (_initializing)
        {
            return;
        }

        try
        {
            var culture = CultureInfo.GetCultureInfo(LangTurkish.IsChecked == true ? "tr-TR" : "en-US");

            await ((App)Application.Current).SetLanguageAsync(culture);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Dil değiştirilemedi.");
        }
    }

    // ------------------------------------------------------------------
    // Arka plan
    // ------------------------------------------------------------------

    private void RebuildBackgrounds()
    {
        var settings = AppServices.Settings;
        var options = new List<BackgroundOption>();

        for (var i = 0; i < BackgroundPresets.All.Count; i++)
        {
            var id = BackgroundPresets.All[i];

            options.Add(new BackgroundOption(
                id,
                Loc.T(PresetNameKeys[i]),
                BackgroundService.LoadPreset(id),
                settings.BackgroundMode == "Preset" && settings.BackgroundPreset == id));
        }

        // Kullanıcının daha önce yüklediği özel görsel varsa onu da seçenek olarak göster.
        var custom = AppServices.Background.LoadCustom(settings.CustomBackgroundFile);

        if (custom is not null)
        {
            options.Add(new BackgroundOption("custom", Loc.T("BackgroundCustom"), custom, settings.BackgroundMode == "Custom"));
        }

        BackgroundList.ItemsSource = options;
    }

    private async void BackgroundOption_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { DataContext: BackgroundOption option })
        {
            return;
        }

        var settings = AppServices.Settings;

        if (option.Id == "custom")
        {
            settings.BackgroundMode = "Custom";
        }
        else
        {
            settings.BackgroundMode = "Preset";
            settings.BackgroundPreset = option.Id;
        }

        await ApplyAndSaveAsync();
    }

    private async void Upload_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = Loc.T("BackgroundUpload"),
            Filter = $"{Loc.T("BackgroundFilter")} (*.jpg;*.jpeg;*.png;*.bmp)|*.jpg;*.jpeg;*.png;*.bmp",
            CheckFileExists = true,
            Multiselect = false
        };

        if (dialog.ShowDialog(Window.GetWindow(this)) != true)
        {
            return;
        }

        var result = AppServices.Background.ImportCustom(dialog.FileName);

        if (!result.Success)
        {
            ShowStatus(Loc.T(result.ErrorKey ?? "BackgroundErrorInvalid"));
            return;
        }

        var settings = AppServices.Settings;
        var previous = settings.CustomBackgroundFile;

        settings.CustomBackgroundFile = result.FileName;
        settings.BackgroundMode = "Custom";

        await ApplyAndSaveAsync();

        // Eski özel görsel artık kullanılmıyor.
        AppServices.Background.DeleteCustom(previous);
    }

    private async void Reset_Click(object sender, RoutedEventArgs e)
    {
        var settings = AppServices.Settings;

        settings.BackgroundMode = "Preset";
        settings.BackgroundPreset = BackgroundPresets.Default;

        await ApplyAndSaveAsync();
    }

    private async Task ApplyAndSaveAsync()
    {
        HideStatus();

        // Görsel hemen uygulanır; kayıt başarısız olsa bile bu oturumda geçerli kalır.
        AppServices.Background.Apply(AppServices.Settings);
        RebuildBackgrounds();

        try
        {
            await AppServices.SaveSettingsAsync();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Arka plan seçimi kaydedilemedi.");
            ShowStatus(ex.Message);
        }
    }

    private void ShowStatus(string text)
    {
        BackgroundStatusText.Text = text;
        BackgroundStatusText.Visibility = Visibility.Visible;
    }

    private void HideStatus()
    {
        BackgroundStatusText.Visibility = Visibility.Collapsed;
    }

    // ------------------------------------------------------------------
    // Veri klasörü
    // ------------------------------------------------------------------

    private void OpenDataFolder_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Directory.CreateDirectory(AppServices.DataDirectory);

            Process.Start(new ProcessStartInfo("explorer.exe", $"\"{AppServices.DataDirectory}\"") { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Veri klasörü açılamadı.");
        }
    }
}
