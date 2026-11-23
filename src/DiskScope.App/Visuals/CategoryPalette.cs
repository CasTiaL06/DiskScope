using System.Windows;
using System.Windows.Media;
using DiskScope.App.Localization;
using DiskScope.Scanning;

namespace DiskScope.App.Visuals;

/// <summary>Dosya kategorilerinin renkleri ve yerelleştirilmiş adları.</summary>
public static class CategoryPalette
{
    private static readonly Dictionary<FileCategory, Color> Colors = new()
    {
        [FileCategory.Video] = Color.FromRgb(0xD6, 0x60, 0x4D),
        [FileCategory.Image] = Color.FromRgb(0x8E, 0x6B, 0xD8),
        [FileCategory.Audio] = Color.FromRgb(0xD3, 0xA2, 0x3F),
        [FileCategory.Document] = Color.FromRgb(0x3E, 0x8F, 0xD6),
        [FileCategory.Archive] = Color.FromRgb(0xD9, 0x84, 0x3F),
        [FileCategory.Application] = Color.FromRgb(0x3F, 0xAE, 0x7C),
        [FileCategory.System] = Color.FromRgb(0x5A, 0x7D, 0xA8),
        [FileCategory.DiskImage] = Color.FromRgb(0xC2, 0x55, 0x9D),
        [FileCategory.Other] = Color.FromRgb(0x6B, 0x76, 0x80)
    };

    private static readonly Dictionary<FileCategory, SolidColorBrush> SolidBrushes = new();
    private static readonly Dictionary<FileCategory, LinearGradientBrush> TileBrushes = new();

    static CategoryPalette()
    {
        foreach (var (category, color) in Colors)
        {
            SolidBrushes[category] = Frozen(new SolidColorBrush(color));

            var gradient = new LinearGradientBrush
            {
                StartPoint = new System.Windows.Point(0, 0),
                EndPoint = new System.Windows.Point(0, 1)
            };

            gradient.GradientStops.Add(new GradientStop(Blend(color, Color.FromRgb(255, 255, 255), 0.10), 0));
            gradient.GradientStops.Add(new GradientStop(Blend(color, Color.FromRgb(0, 0, 0), 0.12), 1));

            TileBrushes[category] = Frozen(gradient);
        }
    }

    /// <summary>Klasör simgeleri için sakin gri-mavi ton.</summary>
    public static SolidColorBrush FolderIconBrush { get; } = Frozen(new SolidColorBrush(Color.FromRgb(0x8B, 0x98, 0xA6)));

    /// <summary>Klasör satırlarındaki yüzde çubuğu (vurgu rengi).</summary>
    public static SolidColorBrush FolderBarBrush { get; } = Frozen(new SolidColorBrush(Color.FromRgb(0x2B, 0xB3, 0xC0)));

    public static SolidColorBrush SmallFilesBrush { get; } = Frozen(new SolidColorBrush(Color.FromRgb(0x5B, 0x66, 0x70)));

    public static SolidColorBrush GetBrush(FileCategory category) =>
        SolidBrushes.TryGetValue(category, out var brush) ? brush : SolidBrushes[FileCategory.Other];

    public static LinearGradientBrush GetTileBrush(FileCategory category) =>
        TileBrushes.TryGetValue(category, out var brush) ? brush : TileBrushes[FileCategory.Other];

    public static string GetName(FileCategory category) => Loc.T("Category" + category);

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
