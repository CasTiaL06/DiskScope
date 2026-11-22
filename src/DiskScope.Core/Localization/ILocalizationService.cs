using System.Globalization;

namespace DiskScope.Core.Localization;

public interface ILocalizationService
{
    CultureInfo CurrentCulture { get; }

    string Get(string key);

    void SetCulture(CultureInfo culture);

    event EventHandler? LanguageChanged;
}
