using System.Windows.Media;

namespace DiskScope.App.Views;

/// <summary>Genel Bakış: kategori dağılımı satırı.</summary>
internal sealed record LegendRow(Brush Brush, string Name, string SizeText, string PercentText);

/// <summary>Genel Bakış: en çok yer kaplayan klasör satırı.</summary>
internal sealed record TopFolderRow(string Name, string SizeText, double Percent);

/// <summary>Son analizler / geçmiş satırı.</summary>
internal sealed record ScanRow(string Path, string Title, string WhenText, string SizeText, string DetailText);

/// <summary>Disk sağlığı sayfası satırı.</summary>
internal sealed record HealthRow(
    string Name,
    string MediaText,
    string StatusText,
    Brush StatusBrush,
    Brush StatusBackground,
    string Glyph,
    string TemperatureText,
    string HoursText,
    string SizeText);

/// <summary>Ayarlar: arka plan seçeneği kartı.</summary>
internal sealed record BackgroundOption(string Id, string DisplayName, ImageSource? Preview, bool IsSelected);
