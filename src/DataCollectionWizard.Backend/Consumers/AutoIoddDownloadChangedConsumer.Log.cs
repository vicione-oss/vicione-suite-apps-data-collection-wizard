using Microsoft.Extensions.Logging;

namespace DataCollectionWizard.Backend.Consumers;

public sealed partial class AutoIoddDownloadChangedConsumer
{
    [LoggerMessage(LogLevel.Error, "Failed to apply DeviceTree")]
    private static partial void LogApplicationFailedError(ILogger logger, Exception exception);

    [LoggerMessage(LogLevel.Debug, "Consume {command} CorrelationId:{correlationId}")]
    private static partial void LogConsume(ILogger logger, string command, Guid? correlationId);

    [LoggerMessage(LogLevel.Debug, "No IO-Link devices found, skipping dataflow generation.")]
    private static partial void LogNoDevices(ILogger logger);
}
