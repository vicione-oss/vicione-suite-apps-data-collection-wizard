using Microsoft.Extensions.Logging;

namespace DataCollectionWizard.Backend.Consumers;

public sealed partial class DeleteDeviceTreeConsumer
{
    [LoggerMessage(LogLevel.Debug, "Consume {command} CorrelationId:{correlationId} DeviceAddress:{deviceAddress}")]
    private static partial void LogConsume(ILogger logger, string command, Guid? correlationId, string deviceAddress);
}
