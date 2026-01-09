using Microsoft.Extensions.Logging;

namespace DataCollectionWizard.Client.Components;

public sealed partial class LiveViewPage
{
    [LoggerMessage(LogLevel.Warning, "Error awaiting deployment: {exceptionType} {message} {stacktrace}")]
    public static partial void LogAwaitingDeploymentWarning(ILogger logger, string exceptionType, string message, string stacktrace);

    [LoggerMessage(LogLevel.Error, "Failed to subscribe all topics for DeviceTree: {exceptionType} {message} {stacktrace}")]
    public static partial void LogSubscribeAllError(ILogger logger, string exceptionType, string message, string stacktrace);

    [LoggerMessage(LogLevel.Warning, "Failed to subscribe to topic {topic} for node {node}: {exceptionType} {message} {stacktrace}")]
    public static partial void LogSubscribeTopicError(ILogger logger, Guid topic, string node, string exceptionType, string message, string stacktrace);

    [LoggerMessage(LogLevel.Error, "Failed to extend DeviceTree: {exceptionType} {message} {stacktrace}")]
    public static partial void LogUpdateDeviceTreeError(ILogger logger, string exceptionType, string message, string stacktrace);
}
