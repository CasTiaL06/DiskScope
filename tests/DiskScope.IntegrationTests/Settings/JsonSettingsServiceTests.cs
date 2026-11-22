using DiskScope.Storage.Settings;

namespace DiskScope.IntegrationTests.Settings;

public class JsonSettingsServiceTests
{
    [Fact]
    public async Task LoadAsync_WhenFileDoesNotExist_ReturnsDefaultSettings()
    {
        var root = CreateTemporaryDirectory();

        try
        {
            var service = new JsonSettingsService(root);

            var settings = await service.LoadAsync();

            Assert.Equal("tr-TR", settings.Language);
            Assert.Equal("System", settings.Theme);
            Assert.Null(settings.LastSelectedDrive);
        }
        finally
        {
            DeleteTemporaryDirectory(root);
        }
    }

    [Fact]
    public async Task SaveAsync_ThenLoadAsync_PreservesSettings()
    {
        var root = CreateTemporaryDirectory();

        try
        {
            var service = new JsonSettingsService(root);
            var expected = new AppSettings
            {
                Language = "en-US",
                Theme = "Dark",
                LastSelectedDrive = "D:\\"
            };

            await service.SaveAsync(expected);
            var actual = await service.LoadAsync();

            Assert.Equal(expected.Language, actual.Language);
            Assert.Equal(expected.Theme, actual.Theme);
            Assert.Equal(expected.LastSelectedDrive, actual.LastSelectedDrive);
        }
        finally
        {
            DeleteTemporaryDirectory(root);
        }
    }

    [Fact]
    public async Task LoadAsync_WhenJsonIsInvalid_ReturnsDefaultSettings()
    {
        var root = CreateTemporaryDirectory();

        try
        {
            await File.WriteAllTextAsync(
                Path.Combine(root, "settings.json"),
                "{ invalid json");

            var service = new JsonSettingsService(root);

            var settings = await service.LoadAsync();

            Assert.Equal("tr-TR", settings.Language);
            Assert.Equal("System", settings.Theme);
            Assert.Null(settings.LastSelectedDrive);
        }
        finally
        {
            DeleteTemporaryDirectory(root);
        }
    }

    [Fact]
    public async Task LoadAsync_WhenJsonIsNull_ReturnsDefaultSettings()
    {
        var root = CreateTemporaryDirectory();

        try
        {
            await File.WriteAllTextAsync(
                Path.Combine(root, "settings.json"),
                "null");

            var service = new JsonSettingsService(root);

            var settings = await service.LoadAsync();

            Assert.Equal("tr-TR", settings.Language);
            Assert.Equal("System", settings.Theme);
            Assert.Null(settings.LastSelectedDrive);
        }
        finally
        {
            DeleteTemporaryDirectory(root);
        }
    }
    [Fact]
    public async Task SaveAsync_WhenSettingsIsNull_ThrowsArgumentNullException()
    {
        var root = CreateTemporaryDirectory();

        try
        {
            var service = new JsonSettingsService(root);

            await Assert.ThrowsAsync<ArgumentNullException>(
                () => service.SaveAsync(null!));
        }
        finally
        {
            DeleteTemporaryDirectory(root);
        }
    }

    [Fact]
    public async Task LoadAsync_WhenCancellationRequested_ThrowsOperationCanceledException()
    {
        var root = CreateTemporaryDirectory();

        try
        {
            var service = new JsonSettingsService(root);
            using var cancellationTokenSource = new CancellationTokenSource();

            await cancellationTokenSource.CancelAsync();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                () => service.LoadAsync(cancellationTokenSource.Token));
        }
        finally
        {
            DeleteTemporaryDirectory(root);
        }
    }

    [Fact]
    public async Task SaveAsync_WhenCancellationRequested_ThrowsOperationCanceledException()
    {
        var root = CreateTemporaryDirectory();

        try
        {
            var service = new JsonSettingsService(root);
            using var cancellationTokenSource = new CancellationTokenSource();

            await cancellationTokenSource.CancelAsync();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                () => service.SaveAsync(
                    new AppSettings(),
                    cancellationTokenSource.Token));
        }
        finally
        {
            DeleteTemporaryDirectory(root);
        }
    }

    [Fact]
    public async Task SaveAsync_WhenCancellationRequested_PreservesExistingFile()
    {
        var root = CreateTemporaryDirectory();

        try
        {
            var service = new JsonSettingsService(root);

            var originalSettings = new AppSettings
            {
                Language = "en-US",
                Theme = "Dark"
            };

            await service.SaveAsync(originalSettings);

            using var cancellationTokenSource = new CancellationTokenSource();
            await cancellationTokenSource.CancelAsync();

            var newSettings = new AppSettings
            {
                Language = "tr-TR",
                Theme = "Light"
            };

            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                () => service.SaveAsync(
                    newSettings,
                    cancellationTokenSource.Token));

            var actualSettings = await service.LoadAsync();

            Assert.Equal("en-US", actualSettings.Language);
            Assert.Equal("Dark", actualSettings.Theme);
        }
        finally
        {
            DeleteTemporaryDirectory(root);
        }
    }
    private static string CreateTemporaryDirectory()
    {
        var path = Path.Combine(
            Path.GetTempPath(),
            $"DiskScope-Test-{Guid.NewGuid():N}");

        Directory.CreateDirectory(path);
        return path;
    }

    private static void DeleteTemporaryDirectory(string path)
    {
        if (Directory.Exists(path))
            Directory.Delete(path, true);
    }
}
