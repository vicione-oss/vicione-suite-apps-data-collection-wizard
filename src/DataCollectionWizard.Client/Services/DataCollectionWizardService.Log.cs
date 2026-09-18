using Microsoft.Extensions.Logging;

namespace DataCollectionWizard.Client.Services;

public sealed partial class DataCollectionWizardService
{
    [LoggerMessage(LogLevel.Warning, "Failed to subscribe to DeviceTree output")]
    private static partial void LogClusterSubscriptionFailed(ILogger logger, Exception exception);

    [LoggerMessage(LogLevel.Error, "Failed to deserialize the device scan result")]
    private static partial void LogScanResultSerializationFailed(ILogger logger, Exception exception);

    [LoggerMessage(LogLevel.Warning, "Did not receive any device scan data.")]
    private static partial void LogNoScanDataReceived(ILogger logger);

    [LoggerMessage(LogLevel.Warning, "Timeout while adding the device scanner engine.")]
    private static partial void LogDeviceScanEngineTimeoutCreatingDataflow(ILogger logger);

    [LoggerMessage(LogLevel.Information, "Skipping first scan message - retained message.")]
    private static partial void LogScanReceivedFirstMessage(ILogger logger);

    [LoggerMessage(LogLevel.Information, "Skipping scan message - output is null.")]
    private static partial void LogScanReceivedNullMessage(ILogger logger);

    [LoggerMessage(LogLevel.Information, "Received second scan message.")]
    private static partial void LogScanReceivedSecondMessage(ILogger logger);

    [LoggerMessage(LogLevel.Warning, "Failed to subscribe to the device scan output, creating the dataflow.")]
    private static partial void LogScannerSubscriptionFailedCreatingDataflow(ILogger logger);

    [LoggerMessage(LogLevel.Error, "Unexpectedly failed to subscribe to the device scan output")]
    private static partial void LogScannerSubscriptionFailedUnexpectedly(ILogger logger, Exception exception);

    [LoggerMessage(LogLevel.Warning, "Did not receive second message: {url} - device is offline.")]
    private static partial void LogTimeoutDidNotReceiveDeviceMessage(ILogger logger, string url);

    [LoggerMessage(LogLevel.Information, "Subscribing DeviceTree for device at {deviceAddress}: {deviceTreeOutputId}, trigger: {deviceTreeTriggerId}")]
    private static partial void LogSubscribingDeviceTree(ILogger logger, Uri deviceAddress, Guid deviceTreeOutputId, Guid deviceTreeTriggerId);

    [LoggerMessage(LogLevel.Information, "Subscribing to the device scan output.")]
    private static partial void LogSubscribingScanOutput(ILogger logger);

    [LoggerMessage(LogLevel.Information, "Sending trigger for device at {deviceAddress}: {deviceTreeTriggerId}")]
    private static partial void LogSendingDeviceTrigger(ILogger logger, Uri deviceAddress, Guid deviceTreeTriggerId);

    [LoggerMessage(LogLevel.Information, "Received first message - retained message, waiting for second message: {url}")]
    private static partial void LogSkipFirstMessageInformation(ILogger logger, Uri url);

    [LoggerMessage(LogLevel.Warning, "Received null device, waiting for second message: {url}")]
    private static partial void LogSkipFirstMessageWarning(ILogger logger, Uri url);

    [LoggerMessage(LogLevel.Information, "Received device message: {url}")]
    private static partial void LogReceivedDeviceMessageInfo(ILogger logger, Uri url);

    [LoggerMessage(LogLevel.Information, "An error occurred requesting Device {url}")]
    private static partial void LogRequestExistingDeviceFailedWarning(ILogger logger, Uri url, Exception exception);

    [LoggerMessage(LogLevel.Debug, "{Name} returns {Count} ids")]
    private static partial void LogReturnsIdsDebug(ILogger logger, string name, int count);

    [LoggerMessage(LogLevel.Debug, "Triggering a device scan.")]
    private static partial void LogTriggeringScan(ILogger logger);

    [LoggerMessage(LogLevel.Warning, "Failed to apply device tree: timeout while waiting for ticket")]
    partial void LogFailedToApplyDeviceTreeTimeoutWhileWaitingForTicket();
}
