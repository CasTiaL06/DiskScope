using System.Globalization;

namespace DiskScope.Core.Formatting;

public static class SizeFormatter
{
    private static readonly string[] Units = ["B", "KB", "MB", "GB", "TB", "PB"];

    /// <summary>Bayt sayısını "1,5 GB" gibi okunaklı metne çevirir (1 KB = 1024 B).</summary>
    public static string Format(long bytes, IFormatProvider? provider = null)
    {
        double size = Math.Max(0, bytes);
        var unit = 0;

        while (size >= 1024 && unit < Units.Length - 1)
        {
            size /= 1024;
            unit++;
        }

        var format = unit == 0 ? "0" : "0.##";

        var culture = provider ?? CultureInfo.CurrentCulture;

        return $"{size.ToString(format, culture)} {Units[unit]}";
    }
}
