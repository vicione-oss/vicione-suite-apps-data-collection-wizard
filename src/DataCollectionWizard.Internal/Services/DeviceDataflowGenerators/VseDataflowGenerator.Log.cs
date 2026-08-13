using Microsoft.Extensions.Logging;

namespace DataCollectionWizard.Internal.Services.DeviceDataflowGenerators;

public sealed partial class VseDataflowGenerator
{
    [LoggerMessage(LogLevel.Warning, "No input connector was found for {subscriberType} subscriber - {childName}")]
    private static partial void LogNoInConSubscriber(ILogger logger, string subscriberType, string childName);

    [LoggerMessage(LogLevel.Warning, "No output connector was found for {subscriberType} subscriber - {childName}")]
    private static partial void LogNoOutConSubscriber(ILogger logger, string subscriberType, string childName);
}
