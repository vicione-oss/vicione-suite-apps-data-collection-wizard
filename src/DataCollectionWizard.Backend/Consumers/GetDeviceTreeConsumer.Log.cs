using Microsoft.Extensions.Logging;

namespace DataCollectionWizard.Backend.Consumers;

public sealed partial class GetDeviceTreeConsumer
{
    [LoggerMessage(LogLevel.Debug, "Respond with DeviceTree from database with {Count} children")]
    private static partial void LogRespondWithDeviceTree(ILogger logger, int count);
}
