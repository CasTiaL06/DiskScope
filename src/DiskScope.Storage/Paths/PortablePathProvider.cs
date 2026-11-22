using System.Security;

namespace DiskScope.Storage.Paths;

public sealed class PortablePathProvider
{
    public string DataDirectory { get; }
    public bool IsPortable { get; }

    public PortablePathProvider(
        string? applicationDirectory = null,
        string? localAppDataDirectory = null)
    {
        var appDirectory = Path.GetFullPath(
            applicationDirectory ?? AppContext.BaseDirectory);

        var localAppData = localAppDataDirectory
            ?? Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        if (string.IsNullOrWhiteSpace(localAppData))
        {
            throw new InvalidOperationException(
                "Local Application Data directory could not be determined.");
        }

        var portableDirectory = Path.Combine(appDirectory, "Data");

        if (TryEnsureWritable(portableDirectory))
        {
            DataDirectory = portableDirectory;
            IsPortable = true;
            return;
        }

        var fallbackDirectory = Path.Combine(
            Path.GetFullPath(localAppData),
            "DiskScope",
            "Data");

        EnsureWritable(fallbackDirectory);

        DataDirectory = fallbackDirectory;
        IsPortable = false;
    }

    private static bool TryEnsureWritable(string directory)
    {
        try
        {
            EnsureWritable(directory);
            return true;
        }
        catch (IOException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
        catch (SecurityException)
        {
            return false;
        }
    }

    private static void EnsureWritable(string directory)
    {
        Directory.CreateDirectory(directory);

        var testFile = Path.Combine(
            directory,
            $".diskscope-write-test-{Guid.NewGuid():N}");

        using (new FileStream(
            testFile,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None))
        {
        }

        try
        {
            File.Delete(testFile);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
