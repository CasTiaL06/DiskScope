using System.Windows;
using System.Windows.Media;
using DiskScope.App.Localization;
using DiskScope.Scanning;

namespace DiskScope.App.Views;

/// <summary>Disk sağlık durumunun metni, simgesi ve renkleri.</summary>
internal static class HealthStyle
{
    public static string Text(DiskHealthStatus status) => Loc.T("Health" + status);

    public static string Glyph(DiskHealthStatus status) => status switch
    {
        DiskHealthStatus.Healthy => "\uE73E",
        DiskHealthStatus.Warning => "\uE7BA",
        DiskHealthStatus.Unhealthy => "\uE711",
        _ => "\uE897"
    };

    public static Brush Foreground(DiskHealthStatus status) => (Brush)Application.Current.Resources[status switch
    {
        DiskHealthStatus.Healthy => "SuccessBrush",
        DiskHealthStatus.Warning => "WarningBrush",
        DiskHealthStatus.Unhealthy => "DangerBrush",
        _ => "TextSecondaryBrush"
    }];

    /// <summary>Durum renginin saydam (arka plan için) hâli.</summary>
    public static Brush Background(DiskHealthStatus status)
    {
        var color = ((SolidColorBrush)Foreground(status)).Color;
        var soft = new SolidColorBrush(Color.FromArgb(0x30, color.R, color.G, color.B));

        soft.Freeze();
        return soft;
    }
}
