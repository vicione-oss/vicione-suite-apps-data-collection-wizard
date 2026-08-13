using Microsoft.Extensions.Logging;

namespace DataCollectionWizard.Client.Components;

public sealed partial class DataCollectionWizardPage
{
    [LoggerMessage(LogLevel.Warning, "Error awaiting deployment")]
    private static partial void LogAwaitingDeploymentWarning(ILogger logger, Exception exception);

    [LoggerMessage(LogLevel.Error, "Error during data collection wizard initialization")]
    private static partial void LogInitDcwError(ILogger logger, Exception exception);

    [LoggerMessage(LogLevel.Warning, "Selected device with missing address")]
    private static partial void LogMissingAddressSelectedWarning(ILogger logger);

    [LoggerMessage(LogLevel.Error, "Error during Rebrowse")]
    private static partial void LogRebrowseButtonError(ILogger logger, Exception exception);

    [LoggerMessage(LogLevel.Error, "Received unexpected value for device {deviceUrl}: null")]
    private static partial void LogUnexpectedNullDeviceError(ILogger logger, string deviceUrl);

    [LoggerMessage(LogLevel.Warning, "Unexpected tree update, overwriting {id}")]
    private static partial void LogUnexpectedUpdateWarning(ILogger logger, string id);

    [LoggerMessage(LogLevel.Error, "Failed to extend devicetree")]
    private static partial void LogUpdateDeviceTreeError(ILogger logger, Exception exception);

    [LoggerMessage(LogLevel.Error, "An error while trying to save DeviceTree (SaveButtonAsync)")]
    private static partial void LogWhileSaveDeviceTreeError(ILogger logger, Exception exception);
}
