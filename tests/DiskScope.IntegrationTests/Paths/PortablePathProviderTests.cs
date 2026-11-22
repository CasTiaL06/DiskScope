using DiskScope.Storage.Paths;

namespace DiskScope.IntegrationTests.Paths;

public class PortablePathProviderTests
{
    [Fact]
    public void UsesPortableDirectory_WhenApplicationDirectoryIsWritable()
    {
        var root = Path.Combine(
            Path.GetTempPath(),
            $"DiskScope-Test-{Guid.NewGuid():N}");

        var appDirectory = Path.Combine(root, "App");
        var localAppData = Path.Combine(root, "LocalAppData");

        try
        {
            var provider = new PortablePathProvider(
                appDirectory,
                localAppData);

            Assert.True(provider.IsPortable);
            Assert.Equal(
                Path.Combine(appDirectory, "Data"),
                provider.DataDirectory);

            Assert.True(Directory.Exists(provider.DataDirectory));
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, true);
        }
    }

    [Fact]
    public void UsesLocalAppData_WhenApplicationDirectoryIsNotWritable()
    {
        var root = Path.Combine(
            Path.GetTempPath(),
            $"DiskScope-Test-{Guid.NewGuid():N}");

        var appDirectory = Path.Combine(root, "App");
        var localAppData = Path.Combine(root, "LocalAppData");

        try
        {
            Directory.CreateDirectory(root);

            // Uygulama klasörü yerine aynı isimde bir dosya oluşturuyoruz.
            // Böylece uygulama klasörünün Data alt klasörü oluşturulamıyor.
            File.WriteAllText(appDirectory, "test");

            var provider = new PortablePathProvider(
                appDirectory,
                localAppData);

            Assert.False(provider.IsPortable);
            Assert.Equal(
                Path.Combine(localAppData, "DiskScope", "Data"),
                provider.DataDirectory);

            Assert.True(Directory.Exists(provider.DataDirectory));
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, true);
        }
    }
}
