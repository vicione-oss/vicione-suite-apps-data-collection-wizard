using Microsoft.Extensions.Logging;

namespace DataCollectionWizard.Backend.Consumers;

public sealed partial class UpdateDeviceTreeConsumer
{
    [LoggerMessage(LogLevel.Debug, "Consume {command} CorrelationId:{correlationId}")]
    private static partial void LogConsume(ILogger logger, string command, Guid? correlationId);
}
