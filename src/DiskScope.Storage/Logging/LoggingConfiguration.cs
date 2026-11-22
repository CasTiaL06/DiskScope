using Serilog;

namespace DiskScope.Storage.Logging;

public static class LoggingConfiguration
{
    public static ILogger Create(string dataDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dataDirectory);

        var logDirectory = Path.Combine(dataDirectory, "Logs");
        Directory.CreateDirectory(logDirectory);

        var logPath = Path.Combine(logDirectory, "diskscope-.log");

        return new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.File(
                logPath,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 14,
                shared: true)
            .CreateLogger();
    }
}
