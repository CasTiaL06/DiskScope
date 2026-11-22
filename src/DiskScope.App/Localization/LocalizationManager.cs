using System.ComponentModel;
using DiskScope.Core.Localization;

namespace DiskScope.App.Localization;

public sealed class LocalizationManager : INotifyPropertyChanged
{
    private static LocalizationManager? _instance;

    private readonly ILocalizationService _service;

    public static LocalizationManager Instance =>
        _instance ?? throw new InvalidOperationException(
            "LocalizationManager has not been initialized.");

    public event PropertyChangedEventHandler? PropertyChanged;

    public string this[string key] => _service.Get(key);

    private LocalizationManager(ILocalizationService service)
    {
        _service = service;
        _service.LanguageChanged += OnLanguageChanged;
    }

    public static void Initialize(ILocalizationService service)
    {
        ArgumentNullException.ThrowIfNull(service);

        if (_instance is not null)
            throw new InvalidOperationException(
                "LocalizationManager is already initialized.");

        _instance = new LocalizationManager(service);
    }

    private void OnLanguageChanged(object? sender, EventArgs e)
    {
        PropertyChanged?.Invoke(
            this,
            new PropertyChangedEventArgs("Item[]"));
    }
}
