namespace DiskScope.Storage.Settings;

public sealed class AppSettings
{
    public string Language { get; set; } = "tr-TR";

    public string Theme { get; set; } = "System";

    public string? LastSelectedDrive { get; set; }

    /// <summary>"Preset" (hazır görsel) veya "Custom" (kullanıcının kendi görseli).</summary>
    public string BackgroundMode { get; set; } = "Preset";

    public string BackgroundPreset { get; set; } = "neon-platter";

    /// <summary>Kullanıcının yüklediği görselin Data\Backgrounds klasöründeki dosya adı (yol değil).</summary>
    public string? CustomBackgroundFile { get; set; }
}
