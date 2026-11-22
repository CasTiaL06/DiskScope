namespace DiskScope.Storage.Settings;

public sealed class AppSettings
{
    public string Language { get; set; } = "tr-TR";

    public string Theme { get; set; } = "System";

    public string? LastSelectedDrive { get; set; }
}
