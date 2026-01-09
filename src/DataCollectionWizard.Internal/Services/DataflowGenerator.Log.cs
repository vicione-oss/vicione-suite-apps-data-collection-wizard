using Microsoft.Extensions.Logging;

namespace DataCollectionWizard.Internal.Services;

public sealed partial class DataflowGenerator
{
    [LoggerMessage(LogLevel.Warning, "RawDataInfo for {sensorId} is missing.")]
    public static partial void LogNoRawDataInfo(ILogger logger, string sensorId);

    [LoggerMessage(LogLevel.Warning, "Error state output for {referenceNodeId} is missing.")]
    public static partial void LogNoReferenceNodeId(ILogger logger, string referenceNodeId);

    [LoggerMessage(LogLevel.Warning, "RawDataInfo for {sensorId} is missing event trigger sensor for {eventTriggerConfigurationId}.")]
    public static partial void LogRawDataInfoMissesEventTriggerSensor(ILogger logger, string sensorId, Guid eventTriggerConfigurationId);

    [LoggerMessage(LogLevel.Warning, "RawDataInfo for {sensorId} is missing scheduled trigger sensor for {eventTriggerConfigurationId}.")]
    public static partial void LogRawDataInfoMissesScheduledTriggerSensor(ILogger logger, string sensorId, Guid eventTriggerConfigurationId);

    [LoggerMessage(LogLevel.Warning, "Could not connect {sensorId} to cloud {connection}, missing cloud input.")]
    public static partial void LogMissingCloudInputForDatapoint(ILogger logger, string sensorId, string connection);
}
