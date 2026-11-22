namespace DiskScope.Storage.Settings;

public static class AppSettingsValidator
{
    private static readonly HashSet<string> SupportedLanguages =
    [
        "tr-TR",
        "en-US"
    ];

    private static readonly HashSet<string> SupportedThemes =
    [
        "System",
        "Light",
        "Dark"
    ];

    public static bool Validate(AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var isValid = true;

        if (!SupportedLanguages.Contains(settings.Language))
        {
            settings.Language = "tr-TR";
            isValid = false;
        }

        if (!SupportedThemes.Contains(settings.Theme))
        {
            settings.Theme = "System";
            isValid = false;
        }

        if (settings.LastSelectedDrive is not null &&
            !IsValidDrive(settings.LastSelectedDrive))
        {
            settings.LastSelectedDrive = null;
            isValid = false;
        }

        return isValid;
    }

    private static bool IsValidDrive(string drive)
    {
        return drive.Length == 3 &&
               char.IsAsciiLetter(drive[0]) &&
               drive[1] == ':' &&
               drive[2] == '\\';
    }
}
