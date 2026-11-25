using System.Text.Json;

namespace DiskScope.Storage.History;

/// <summary>
/// Bir listeyi JSON dosyasında saklar (taramalar geçmişi, günlük doluluk kayıtları vb.).
/// Yazma işlemi atomiktir: önce geçici dosyaya yazılır, sonra yerine taşınır.
/// Bozuk dosya uygulamayı durdurmaz; boş liste döner.
/// </summary>
public sealed class JsonListStore<T>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    private readonly string _filePath;

    public JsonListStore(string dataDirectory, string fileName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dataDirectory);
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);

        _filePath = Path.Combine(dataDirectory, fileName);
    }

    public async Task<List<T>> LoadAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!File.Exists(_filePath))
        {
            return new List<T>();
        }

        try
        {
            await using var stream = File.OpenRead(_filePath);

            return await JsonSerializer.DeserializeAsync<List<T>>(stream, JsonOptions, cancellationToken)
                   ?? new List<T>();
        }
        catch (JsonException)
        {
            return new List<T>();
        }
    }

    public async Task SaveAsync(IReadOnlyList<T> items, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(items);
        cancellationToken.ThrowIfCancellationRequested();

        var directory = Path.GetDirectoryName(_filePath);

        if (string.IsNullOrWhiteSpace(directory))
        {
            throw new InvalidOperationException("Data directory is invalid.");
        }

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
                await JsonSerializer.SerializeAsync(stream, items, JsonOptions, cancellationToken);
                await stream.FlushAsync(cancellationToken);
            }

            File.Move(temporaryPath, _filePath, true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }
}
