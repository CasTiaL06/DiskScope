using System.Windows;
using System.Windows.Media;
using DiskScope.App.Localization;
using DiskScope.Scanning;

namespace DiskScope.App.Visuals;

/// <summary>Dosya kategorilerinin renkleri, fırçaları ve yerelleştirilmiş adları.</summary>
public static class CategoryPalette
{
    private static readonly Dictionary<FileCategory, Color> Colors = new()
    {
        [FileCategory.Video] = Color.FromRgb(0xE5, 0x48, 0x4D),
        [FileCategory.Image] = Color.FromRgb(0xA1, 0x68, 0xF0),
        [FileCategory.Audio] = Color.FromRgb(0xF2, 0xC0, 0x3F),
        [FileCategory.Document] = Color.FromRgb(0x3B, 0x82, 0xF6),
        [FileCategory.Archive] = Color.FromRgb(0xF0, 0x8A, 0x24),
        [FileCategory.Application] = Color.FromRgb(0x46, 0xB9, 0x68),
        [FileCategory.System] = Color.FromRgb(0x5B, 0x7F, 0xA6),
        [FileCategory.DiskImage] = Color.FromRgb(0xE0, 0x52, 0x9C),
        [FileCategory.Game] = Color.FromRgb(0x2B, 0xC4, 0xD2),
        [FileCategory.Other] = Color.FromRgb(0x6B, 0x76, 0x80)
    };

    private static readonly Dictionary<Color, SolidColorBrush> SolidCache = new();
    private static readonly Dictionary<Color, LinearGradientBrush> TileCache = new();

    /// <summary>Klasör simgeleri için sakin gri-mavi ton.</summary>
    public static SolidColorBrush FolderIconBrush { get; } = Frozen(new SolidColorBrush(Color.FromRgb(0x8B, 0x98, 0xA6)));

    /// <summary>Klasör satırlarındaki yüzde çubuğu (vurgu rengi).</summary>
    public static SolidColorBrush FolderBarBrush { get; } = Frozen(new SolidColorBrush(Color.FromRgb(0x2B, 0xB3, 0xC0)));

    public static SolidColorBrush SmallFilesBrush { get; } = Frozen(new SolidColorBrush(Color.FromRgb(0x5B, 0x66, 0x70)));

    public static Color GetColor(FileCategory category) =>
        Colors.TryGetValue(category, out var color) ? color : Colors[FileCategory.Other];

    public static SolidColorBrush GetBrush(FileCategory category) => CreateBrush(GetColor(category));

    public static LinearGradientBrush GetTileBrush(FileCategory category) => CreateTileBrush(GetColor(category));

    public static string GetName(FileCategory category) => Loc.T("Category" + category);

    /// <summary>Verilen renk için (önbellekli, dondurulmuş) düz fırça.</summary>
    public static SolidColorBrush CreateBrush(Color color)
    {
        if (!SolidCache.TryGetValue(color, out var brush))
        {
            brush = Frozen(new SolidColorBrush(color));
            SolidCache[color] = brush;
        }

        return brush;
    }

    /// <summary>Verilen renk için, üstü hafif parlak bir degrade "cam" fırça (treemap karoları için).</summary>
    public static LinearGradientBrush CreateTileBrush(Color color)
    {
        if (!TileCache.TryGetValue(color, out var brush))
        {
            var gradient = new LinearGradientBrush
            {
                StartPoint = new Point(0, 0),
                EndPoint = new Point(0, 1)
            };

            gradient.GradientStops.Add(new GradientStop(Blend(color, Color.FromRgb(255, 255, 255), 0.16), 0));
            gradient.GradientStops.Add(new GradientStop(color, 0.45));
            gradient.GradientStops.Add(new GradientStop(Blend(color, Color.FromRgb(0, 0, 0), 0.22), 1));

            brush = Frozen(gradient);
            TileCache[color] = brush;
        }

        return brush;
    }

    private static Color Blend(Color from, Color to, double amount) => Color.FromRgb(
        (byte)(from.R + (to.R - from.R) * amount),
        (byte)(from.G + (to.G - from.G) * amount),
        (byte)(from.B + (to.B - from.B) * amount));

    private static T Frozen<T>(T freezable) where T : Freezable
    {
        freezable.Freeze();
        return freezable;
    }
}
