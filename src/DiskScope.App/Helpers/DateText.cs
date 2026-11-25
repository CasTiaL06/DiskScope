using System.Globalization;
using DiskScope.App.Localization;

namespace DiskScope.App.Helpers;

internal static class DateText
{
    /// <summary>"Bugün • 14:37", "Dün • 22:18", "2 gün önce" gibi göreli tarih metni.</summary>
    public static string Describe(DateTime utc)
    {
        var local = utc.ToLocalTime();
        var days = (DateTime.Now.Date - local.Date).Days;
        var time = local.ToString("t", CultureInfo.CurrentCulture);

        if (days <= 0)
        {
            return Loc.F("DateToday", time);
        }

        if (days == 1)
        {
            return Loc.F("DateYesterday", time);
        }

        return days < 30
            ? Loc.F("DateDaysAgo", days)
            : local.ToString("d", CultureInfo.CurrentCulture);
    }
}
