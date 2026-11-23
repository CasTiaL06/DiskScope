namespace DiskScope.App.Helpers;

internal static class DriveHelper
{
    /// <summary>
    /// Yol bir disk kökü ise (örneğin "C:\") diskte kullanılan bayt sayısını döndürür.
    /// Klasörler için null döner (ilerleme yüzdesi hesaplanamaz).
    /// </summary>
    public static long? GetUsedBytesIfRoot(string path)
    {
        try
        {
            var full = Path.GetFullPath(path);
            var root = Path.GetPathRoot(full);

            if (string.IsNullOrEmpty(root) || !string.Equals(full, root, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            var drive = new DriveInfo(root);

            return drive.IsReady ? drive.TotalSize - drive.AvailableFreeSpace : null;
        }
        catch (Exception)
        {
            return null;
        }
    }
}
