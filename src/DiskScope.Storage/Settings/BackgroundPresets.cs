namespace DiskScope.Storage.Settings;

/// <summary>Uygulamayla gelen hazır arka plan görselleri.</summary>
public static class BackgroundPresets
{
    public const string Default = "neon-platter";

    public static readonly IReadOnlyList<string> All = ["neon-platter", "aurora", "grid", "waves"];

    public static bool IsKnown(string? id) => id is not null && All.Contains(id);
}
