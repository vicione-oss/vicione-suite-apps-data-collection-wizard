using Microsoft.Extensions.Logging;

namespace DataCollectionWizard.Backend.Consumers;

public sealed partial class AddIoLinkScannerConsumer
{
    [LoggerMessage(LogLevel.Debug, "Consume {command} CorrelationId:{correlationId}")]
    private static partial void LogConsume(ILogger logger, string command, Guid? correlationId);

    [LoggerMessage(LogLevel.Error, "Error trying to add IO-Link scanner engine")]
    private static partial void LogError(ILogger logger, Exception exception);
}
