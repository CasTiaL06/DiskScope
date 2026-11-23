using System.Globalization;

namespace DiskScope.App.Localization;

/// <summary>Kodtan (XAML dışından) yerelleştirilmiş metin almak için kısayol.</summary>
public static class Loc
{
    public static string T(string key) => LocalizationManager.Instance[key];

    public static string F(string key, params object[] args) =>
        string.Format(CultureInfo.CurrentCulture, T(key), args);
}
