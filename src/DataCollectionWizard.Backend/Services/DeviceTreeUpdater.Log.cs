using Microsoft.Extensions.Logging;

namespace DataCollectionWizard.Backend.Services;

public sealed partial class DeviceTreeUpdater
{
    [LoggerMessage(LogLevel.Error, "Failed to apply DeviceTree")]
    private static partial void LogApplicationFailedError(ILogger logger, Exception exception);
}
