using Microsoft.Extensions.Logging;

namespace DataCollectionWizard.Backend.Consumers;

public sealed partial class DeleteOutputConnectorMappingConsumer
{
    [LoggerMessage(LogLevel.Debug, "Consume {command} CorrelationId:{correlationId} ProcessDataIds.Count:{count}")]
    private static partial void LogConsume(ILogger logger, string command, Guid? correlationId, int count);
}
