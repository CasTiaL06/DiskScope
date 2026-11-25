using System.Globalization;
using System.Windows;
using System.Windows.Threading;
using DiskScope.App.Localization;
using DiskScope.App.Services;
using DiskScope.Storage.Logging;
using DiskScope.Storage.Paths;
using DiskScope.Storage.Settings;
using Serilog;

namespace DiskScope.App;

public partial class App : Application
{
    public LocalizationService Localization { get; private set; } = null!;

    private JsonSettingsService _settingsService = null!;
    private AppSettings _settings = null!;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        RegisterGlobalExceptionHandlers();

        try
        {
            var paths = new PortablePathProvider();

            Log.Logger = LoggingConfiguration.Create(paths.DataDirectory);

            Log.Information(
                "NexDisk başlıyor. Taşınabilir mod: {IsPortable}, veri klasörü: {DataDirectory}",
                paths.IsPortable,
                paths.DataDirectory);

            _settingsService = new JsonSettingsService(paths.DataDirectory);

            _settings = await _settingsService.LoadAsync();

            var settingsAreValid = AppSettingsValidator.Validate(_settings);

            if (!settingsAreValid)
            {
                await _settingsService.SaveAsync(_settings);
            }

            Localization = new LocalizationService();

            Localization.SetCulture(
                CultureInfo.GetCultureInfo(_settings.Language));

            LocalizationManager.Initialize(Localization);

            AppServices.Initialize(paths.DataDirectory, _settingsService, _settings);

            var mainWindow = new MainWindow();
            mainWindow.Show();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "NexDisk başlatılamadı.");

            // Dil sistemi henüz hazır olmayabilir; bu yüzden mesaj iki dillidir.
            MessageBox.Show(
                "NexDisk başlatılamadı. / NexDisk could not start.\n\n" + ex.Message,
                "NexDisk",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            Shutdown(1);
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Log.Information("NexDisk kapanıyor.");
        Log.CloseAndFlush();

        base.OnExit(e);
    }

    public async Task SetLanguageAsync(CultureInfo culture)
    {
        Localization.SetCulture(culture);

        _settings.Language = culture.Name;

        await _settingsService.SaveAsync(_settings);
    }

    private void RegisterGlobalExceptionHandlers()
    {
        DispatcherUnhandledException += OnDispatcherUnhandledException;

        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
            Log.Fatal(args.ExceptionObject as Exception, "Yakalanmamış hata (AppDomain).");

        TaskScheduler.UnobservedTaskException += (_, args) =>
        {
            Log.Error(args.Exception, "Gözlenmemiş görev hatası.");
            args.SetObserved();
        };
    }

    private void OnDispatcherUnhandledException(
        object sender,
        DispatcherUnhandledExceptionEventArgs e)
    {
        Log.Error(e.Exception, "Arayüzde yakalanmamış hata.");

        var title = SafeGet("ErrorUnexpectedTitle", "Beklenmeyen hata / Unexpected error");
        var body = SafeGet("ErrorUnexpectedBody", "Bir hata oluştu. / An error occurred.");

        MessageBox.Show(body, title, MessageBoxButton.OK, MessageBoxImage.Warning);

        e.Handled = true;
    }

    private string SafeGet(string key, string fallback)
    {
        try
        {
            return Localization?.Get(key) ?? fallback;
        }
        catch (Exception)
        {
            return fallback;
        }
    }
}
