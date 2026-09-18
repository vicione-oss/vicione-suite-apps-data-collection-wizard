using Microsoft.Extensions.Logging;

namespace DataCollectionWizard.Backend.Consumers;

public sealed partial class UpsertDeviceConnectorIdsConsumer
{
    [LoggerMessage(LogLevel.Debug, "Consume {command} CorrelationId:{correlationId} Items.Count:{count}")]
    private static partial void LogConsume(ILogger logger, string command, Guid? correlationId, int count);
}
