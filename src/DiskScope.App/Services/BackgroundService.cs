using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using DiskScope.Storage.Settings;
using Serilog;

namespace DiskScope.App.Services;

/// <summary>Sonuç: özel görsel içe aktarma işlemi.</summary>
internal sealed record ImportResult(bool Success, string? FileName, string? ErrorKey);

/// <summary>
/// Genel Bakış üst bölümündeki arka plan görselini yönetir: hazır görseller ve kullanıcının kendi görseli.
/// Özel görseller Data\Backgrounds klasörüne kopyalanır; böylece özgün dosya taşınsa bile seçim kalıcıdır.
/// </summary>
internal sealed class BackgroundService
{
    private const long MaxBytes = 25L * 1024 * 1024;

    private static readonly string[] AllowedExtensions = [".jpg", ".jpeg", ".png", ".bmp"];

    private readonly string _folder;

    public BackgroundService(string dataDirectory)
    {
        _folder = Path.Combine(dataDirectory, "Backgrounds");
    }

    /// <summary>Şu an kullanılan arka plan görseli.</summary>
    public ImageSource? Current { get; private set; }

    public event EventHandler? Changed;

    /// <summary>Ayarlardaki seçimi yükler; görsel okunamazsa varsayılan hazır görsele döner.</summary>
    public void Apply(AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        Current = TryLoadSelected(settings) ?? LoadPreset(BackgroundPresets.Default);
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public static ImageSource? LoadPreset(string id) =>
        Load(new Uri($"pack://application:,,,/Assets/Backgrounds/{id}.jpg"));

    /// <summary>Daha önce içe aktarılmış özel görseli yükler (önizleme için); yoksa null.</summary>
    public ImageSource? LoadCustom(string? fileName)
    {
        if (string.IsNullOrEmpty(fileName) || fileName != Path.GetFileName(fileName))
        {
            return null;
        }

        var path = Path.Combine(_folder, fileName);

        return File.Exists(path) ? Load(new Uri(path)) : null;
    }

    /// <summary>Kullanıcının görselini doğrular ve Data\Backgrounds klasörüne kopyalar.</summary>
    public ImportResult ImportCustom(string sourcePath)
    {
        try
        {
            if (!File.Exists(sourcePath))
            {
                return new ImportResult(false, null, "BackgroundErrorInvalid");
            }

            var extension = Path.GetExtension(sourcePath).ToLowerInvariant();

            if (!AllowedExtensions.Contains(extension))
            {
                return new ImportResult(false, null, "BackgroundErrorFormat");
            }

            if (new FileInfo(sourcePath).Length > MaxBytes)
            {
                return new ImportResult(false, null, "BackgroundErrorSize");
            }

            if (Load(new Uri(sourcePath)) is null)
            {
                return new ImportResult(false, null, "BackgroundErrorInvalid");
            }

            Directory.CreateDirectory(_folder);

            var fileName = $"custom-{Guid.NewGuid():N}{extension}";
            File.Copy(sourcePath, Path.Combine(_folder, fileName), overwrite: false);

            return new ImportResult(true, fileName, null);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Arka plan görseli içe aktarılamadı: {Path}", sourcePath);
            return new ImportResult(false, null, "BackgroundErrorInvalid");
        }
    }

    /// <summary>Artık kullanılmayan özel görseli siler (sessizce; başarısız olursa önemli değil).</summary>
    public void DeleteCustom(string? fileName)
    {
        if (string.IsNullOrEmpty(fileName) || fileName != Path.GetFileName(fileName))
        {
            return;
        }

        try
        {
            File.Delete(Path.Combine(_folder, fileName));
        }
        catch (Exception ex)
        {
            Log.Debug(ex, "Eski arka plan görseli silinemedi: {File}", fileName);
        }
    }

    private ImageSource? TryLoadSelected(AppSettings settings)
    {
        if (settings.BackgroundMode == "Custom" && settings.CustomBackgroundFile is { } file)
        {
            var path = Path.Combine(_folder, file);

            if (File.Exists(path))
            {
                var custom = Load(new Uri(path));

                if (custom is not null)
                {
                    return custom;
                }
            }
        }

        return LoadPreset(settings.BackgroundPreset);
    }

    private static ImageSource? Load(Uri uri)
    {
        try
        {
            var bitmap = new BitmapImage();

            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;   // Dosyayı serbest bırakır (silinebilsin).
            bitmap.DecodePixelWidth = 1920;                  // Çok büyük fotoğraflar belleği şişirmesin.
            bitmap.UriSource = uri;
            bitmap.EndInit();
            bitmap.Freeze();

            return bitmap;
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Görsel yüklenemedi: {Uri}", uri);
            return null;
        }
    }
}
