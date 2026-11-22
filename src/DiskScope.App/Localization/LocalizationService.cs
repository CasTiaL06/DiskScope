using System.Globalization;
using System.Resources;
using DiskScope.Core.Localization;

namespace DiskScope.App.Localization;

public sealed class LocalizationService : ILocalizationService
{
    private static readonly CultureInfo DefaultCulture =
        CultureInfo.GetCultureInfo("tr-TR");

    private readonly ResourceManager _resourceManager;

    public CultureInfo CurrentCulture { get; private set; }

    public event EventHandler? LanguageChanged;

    public LocalizationService()
    {
        _resourceManager = new ResourceManager(
            "DiskScope.App.Resources.Strings",
            typeof(LocalizationService).Assembly);

        CurrentCulture = DefaultCulture;

        ApplyCulture(CurrentCulture);
    }

    public string Get(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        return _resourceManager.GetString(key, CurrentCulture) ?? key;
    }

    public void SetCulture(CultureInfo culture)
    {
        ArgumentNullException.ThrowIfNull(culture);

        if (culture.Name is not ("tr-TR" or "en-US"))
        {
            throw new NotSupportedException(
                $"Culture '{culture.Name}' is not supported.");
        }

        if (CurrentCulture.Name == culture.Name)
            return;

        CurrentCulture = culture;

        ApplyCulture(culture);

        LanguageChanged?.Invoke(this, EventArgs.Empty);
    }

    private static void ApplyCulture(CultureInfo culture)
    {
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;

        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;
    }
}
