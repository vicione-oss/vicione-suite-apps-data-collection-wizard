using Microsoft.Extensions.Logging;

namespace DataCollectionWizard.Backend.Consumers;

public sealed partial class AddDeviceTreeEngineConsumer
{
    [LoggerMessage(LogLevel.Debug, "Consume {command} CorrelationId:{correlationId} {deviceEngineInfos}")]
    private static partial void LogConsume(ILogger logger, string command, Guid? correlationId, string deviceEngineInfos);

    [LoggerMessage(LogLevel.Error, "Error trying to add DeviceTreeRequestEngine")]
    private static partial void LogError(ILogger logger, Exception exception);
}
