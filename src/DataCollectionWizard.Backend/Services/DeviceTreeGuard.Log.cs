using Microsoft.Extensions.Logging;

namespace DataCollectionWizard.Backend.Services;

public sealed partial class DeviceTreeGuard
{
    [LoggerMessage(LogLevel.Warning, "Failed to check for DeviceTree updates for device {masterAddress}, no DeviceTree connectors")]
    private static partial void LogNoDeviceTreeConnectorWarning(ILogger logger, Uri masterAddress);

    [LoggerMessage(LogLevel.Information, "Received DeviceTree event, triggering NodesOfflineEvent for {offlineNodes} nodes")]
    private static partial void LogTriggeringNodesOffline(ILogger logger, int offlineNodes);

    [LoggerMessage(LogLevel.Information, "Detected DeviceTreeApplication, triggering NodesOfflineEvent for {offlineNodes} nodes")]
    private static partial void LogTriggeringNodesOfflineDeviceTreeApplied(ILogger logger, int offlineNodes);

    [LoggerMessage(LogLevel.Information, "Received DeviceTree event, master is offline, triggering NodesOfflineEvent for {offlineNodes} nodes")]
    private static partial void LogTriggeringNodesOfflineMasterOffline(ILogger logger, int offlineNodes);

    [LoggerMessage(LogLevel.Information, "Received DeviceTree event, triggering NodesOnlineEvent for {offlineNodes} nodes")]
    private static partial void LogTriggeringNodesOnline(ILogger logger, int offlineNodes);

    [LoggerMessage(LogLevel.Information, "Updating DeviceTree guard subscriptions")]
    private static partial void LogUpdatingDeviceTreeGuardSubscriptions(ILogger logger);

    [LoggerMessage(LogLevel.Information, "DeviceTreeGuard received event for a device that is not currently in DeviceTree")]
    private static partial void LogWrongDeviceNotInDeviceTreeInformation(ILogger logger);

    [LoggerMessage(LogLevel.Information, "DeviceTreeGuard received event but did not return IDeviceTreeMasterDevice")]
    private static partial void LogWrongDeviceTypeInformation(ILogger logger);

    [LoggerMessage(LogLevel.Warning, "Failed to dispose subscription handle during DeviceTreeGuard disposal")]
    private static partial void LogDisposeSubscriptionHandleFailed(ILogger logger, Exception exception);
}
