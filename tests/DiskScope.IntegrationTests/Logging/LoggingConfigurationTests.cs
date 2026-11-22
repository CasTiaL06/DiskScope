using DiskScope.Storage.Logging;
using Serilog;

namespace DiskScope.IntegrationTests.Logging;

public sealed class LoggingConfigurationTests
{
    [Fact]
    public void Create_WritesLogToDailyFile()
    {
        var dataDirectory = Path.Combine(
            Path.GetTempPath(),
            $"DiskScope-Logging-Test-{Guid.NewGuid():N}");

        try
        {
            var logger = LoggingConfiguration.Create(dataDirectory);

            try
            {
                logger.Information("DiskScope logging test message");
            }
            finally
            {
                (logger as IDisposable)?.Dispose();
            }

            var logDirectory = Path.Combine(dataDirectory, "Logs");
            var logFiles = Directory.GetFiles(logDirectory, "diskscope-*.log");

            Assert.NotEmpty(logFiles);

            var logContent = File.ReadAllText(logFiles[0]);

            Assert.Contains("DiskScope logging test message", logContent);
        }
        finally
        {
            if (Directory.Exists(dataDirectory))
                Directory.Delete(dataDirectory, recursive: true);
        }
    }
}
