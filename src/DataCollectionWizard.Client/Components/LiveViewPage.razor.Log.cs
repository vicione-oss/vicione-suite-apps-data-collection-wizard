using Microsoft.Extensions.Logging;

namespace DataCollectionWizard.Client.Components;

public sealed partial class LiveViewPage
{
    [LoggerMessage(LogLevel.Warning, "Error awaiting deployment")]
    private static partial void LogAwaitingDeploymentWarning(ILogger logger, Exception exception);

    [LoggerMessage(LogLevel.Error, "Failed to subscribe all topics for DeviceTree")]
    private static partial void LogSubscribeAllError(ILogger logger, Exception exception);

    [LoggerMessage(LogLevel.Warning, "Failed to subscribe to topic {topic} for node {node}")]
    private static partial void LogSubscribeTopicError(ILogger logger, Guid topic, string node, Exception exception);

    [LoggerMessage(LogLevel.Error, "Failed to extend DeviceTree")]
    private static partial void LogUpdateDeviceTreeError(ILogger logger, Exception exception);
}
