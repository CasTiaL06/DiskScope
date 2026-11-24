using System.Windows.Media;
using DiskScope.Scanning;

namespace DiskScope.App.Visuals;

/// <summary>
/// En büyük dosya uzantılarına canlı, birbirinden ayırt edilebilir renkler verir
/// (treemap ve sağ paneldeki uzantı listesi aynı renkleri kullanır).
/// Paletin dışında kalan uzantılar kategorilerinin rengini alır.
/// </summary>
public static class ExtensionPalette
{
    private static readonly Color[] Palette =
    [
        Color.FromRgb(0xE5, 0x48, 0x4D), // kırmızı
        Color.FromRgb(0xF0, 0x8A, 0x24), // turuncu
        Color.FromRgb(0xF2, 0xC0, 0x3F), // sarı
        Color.FromRgb(0x46, 0xB9, 0x68), // yeşil
        Color.FromRgb(0x2B, 0xC4, 0xD2), // turkuaz
        Color.FromRgb(0x38, 0xA3, 0xF0), // gök mavisi
        Color.FromRgb(0x4F, 0x6D, 0xF5), // mavi
        Color.FromRgb(0x9B, 0x5D, 0xE5), // mor
        Color.FromRgb(0xE0, 0x52, 0x9C), // pembe
        Color.FromRgb(0xFF, 0x7A, 0x90), // gül
        Color.FromRgb(0x5E, 0xE0, 0xB0), // nane
        Color.FromRgb(0xB5, 0xC9, 0x3A)  // limon
    ];

    private static Dictionary<string, Color> _assigned = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Yeni bir tarama sonucu için renkleri yeniden dağıtır (boyuta göre sıralı liste beklenir).</summary>
    public static void Assign(IEnumerable<ExtensionStat> extensions)
    {
        var map = new Dictionary<string, Color>(StringComparer.OrdinalIgnoreCase);
        var index = 0;

        foreach (var stat in extensions)
        {
            if (index >= Palette.Length)
            {
                break;
            }

            if (stat.Size <= 0 || stat.Extension.Length == 0)
            {
                continue;
            }

            map[stat.Extension] = Palette[index++];
        }

        _assigned = map;
    }

    public static Color GetColor(string? extension, FileCategory category) =>
        !string.IsNullOrEmpty(extension) && _assigned.TryGetValue(extension, out var color)
            ? color
            : CategoryPalette.GetColor(category);

    public static SolidColorBrush GetBrush(string? extension, FileCategory category) =>
        CategoryPalette.CreateBrush(GetColor(extension, category));

    public static LinearGradientBrush GetTileBrush(string? extension, FileCategory category) =>
        CategoryPalette.CreateTileBrush(GetColor(extension, category));
}
