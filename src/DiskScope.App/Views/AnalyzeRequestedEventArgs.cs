namespace DiskScope.App.Views;

public sealed class AnalyzeRequestedEventArgs : EventArgs
{
    public AnalyzeRequestedEventArgs(string path, long? expectedBytes)
    {
        Path = path;
        ExpectedBytes = expectedBytes;
    }

    public string Path { get; }

    /// <summary>Tahmini toplam bayt (disk kökü taranıyorsa kullanılan alan); ilerleme yüzdesi için.</summary>
    public long? ExpectedBytes { get; }
}
