using Microsoft.Extensions.Logging;

namespace DataCollectionWizard.Client.Services;

public sealed partial class DataCollectionWizardService
{
    [LoggerMessage(LogLevel.Warning, "Failed to subscribe to DeviceTree output")]
    private static partial void LogClusterSubscriptionFailed(ILogger logger, Exception exception);

    [LoggerMessage(LogLevel.Error, "Failed to deserialize DCP scan result")]
    private static partial void LogDcpResultSerializationFailed(ILogger logger, Exception exception);

    [LoggerMessage(LogLevel.Warning, "Did not receive DCP scan data.")]
    private static partial void LogNoDcpDataReceived(ILogger logger);

    [LoggerMessage(LogLevel.Warning, "Timeout while adding IO-Link scan engine.")]
    private static partial void LogIoLinkScanEngineTimeoutCreatingDataflow(ILogger logger);

    [LoggerMessage(LogLevel.Information, "Skipping first scan message - retained message.")]
    private static partial void LogIoLinkScanReceivedFirstMessage(ILogger logger);

    [LoggerMessage(LogLevel.Information, "Skipping scan message - output is null.")]
    private static partial void LogIoLinkScanReceivedNullMessage(ILogger logger);

    [LoggerMessage(LogLevel.Information, "Received second scan message.")]
    private static partial void LogIoLinkScanReceivedSecondMessage(ILogger logger);

    [LoggerMessage(LogLevel.Warning, "Failed to subscribe to scan devices output, creating dataflow.")]
    private static partial void LogIoLinkScannerSubscriptionFailedCreatingDataflow(ILogger logger);

    [LoggerMessage(LogLevel.Error, "Unexpectedly failed to subscribe to scan output")]
    private static partial void LogIoLinkScannerSubscriptionFailedUnexpectedly(ILogger logger, Exception exception);

    [LoggerMessage(LogLevel.Warning, "Did not receive second message: {url} - device is offline.")]
    private static partial void LogTimeoutDidNotReceiveDeviceMessage(ILogger logger, string url);

    [LoggerMessage(LogLevel.Information, "Subscribing DeviceTree for device at {deviceAddress}: {deviceTreeOutputId}, trigger: {deviceTreeTriggerId}")]
    private static partial void LogSubscribingDeviceTree(ILogger logger, Uri deviceAddress, Guid deviceTreeOutputId, Guid deviceTreeTriggerId);

    [LoggerMessage(LogLevel.Information, "Subscribing DCP scan output.")]
    private static partial void LogSubscribingIoLinkScanOutput(ILogger logger);

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

    [LoggerMessage(LogLevel.Debug, "Triggering IO-Link master scan.")]
    private static partial void LogTriggeringIoLinkMasterScan(ILogger logger);

    [LoggerMessage(LogLevel.Warning, "Failed to apply device tree: timeout while waiting for ticket")]
    partial void LogFailedToApplyDeviceTreeTimeoutWhileWaitingForTicket();
}
