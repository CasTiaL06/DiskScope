namespace DiskScope.Scanning;

public interface IDiskMediaTypeDetector
{
    /// <param name="driveRoot">Sürücü kökü, örneğin "C:\".</param>
    DiskMediaType Detect(string driveRoot);
}
