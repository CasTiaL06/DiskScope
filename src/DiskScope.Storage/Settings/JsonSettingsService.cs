using System.Text.Json;

namespace DiskScope.Storage.Settings;

public sealed class JsonSettingsService
{
    private readonly string _filePath;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public JsonSettingsService(string dataDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dataDirectory);

        _filePath = Path.Combine(dataDirectory, "settings.json");
    }

    public async Task<AppSettings> LoadAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!File.Exists(_filePath))
            return new AppSettings();

        try
        {
            await using var stream = File.OpenRead(_filePath);

            return await JsonSerializer.DeserializeAsync<AppSettings>(
                stream,
                JsonOptions,
                cancellationToken) ?? new AppSettings();
        }
        catch (JsonException)
        {
            return new AppSettings();
        }
    }

    public async Task SaveAsync(
        AppSettings settings,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(settings);
        cancellationToken.ThrowIfCancellationRequested();

        var directory = Path.GetDirectoryName(_filePath);

        if (string.IsNullOrWhiteSpace(directory))
            throw new InvalidOperationException("Settings directory is invalid.");

        Directory.CreateDirectory(directory);

        var temporaryPath = _filePath + ".tmp";

        try
        {
            await using (var stream = new FileStream(
                temporaryPath,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None,
                4096,
                FileOptions.Asynchronous))
            {
                await JsonSerializer.SerializeAsync(
                    stream,
                    settings,
                    JsonOptions,
                    cancellationToken);

                await stream.FlushAsync(cancellationToken);
            }

            cancellationToken.ThrowIfCancellationRequested();

            File.Move(temporaryPath, _filePath, true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
                File.Delete(temporaryPath);
        }
    }
}
