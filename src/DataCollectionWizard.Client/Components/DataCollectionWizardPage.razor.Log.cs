using Microsoft.Extensions.Logging;

namespace DataCollectionWizard.Client.Components;

public sealed partial class DataCollectionWizardPage
{
    [LoggerMessage(LogLevel.Warning, "Error awaiting deployment: {exceptionType} {message} {stacktrace}")]
    public static partial void LogAwaitingDeploymentWarning(ILogger logger, string exceptionType, string message, string stacktrace);

    [LoggerMessage(LogLevel.Error, "Error during data colletion wizard initialization: {exceptionType} {message} {stacktrace}")]
    public static partial void LogInitDcwError(ILogger logger, string exceptionType, string message, string? stacktrace);

    [LoggerMessage(LogLevel.Error, "Error during Rebrowse: {exceptionType} {message} {stacktrace}")]
    public static partial void LogRebrowseButtonError(ILogger logger, string exceptionType, string message, string stacktrace);

    [LoggerMessage(LogLevel.Warning, "Selected device with missing address")]
    public static partial void LogMissingAddressSelectedWarning(ILogger logger);

    [LoggerMessage(LogLevel.Error, "Received unexpected value for device {deviceUrl}: null")]
    public static partial void LogUnexpectedNullDeviceError(ILogger logger, string deviceUrl);

    [LoggerMessage(LogLevel.Warning, "Unexpected tree update, overwriting {id}")]
    public static partial void LogUnexpectedUpdateWarning(ILogger logger, string id);

    [LoggerMessage(LogLevel.Error, "Failed to extend devicetree: {exceptionType} {message} {stacktrace}")]
    public static partial void LogUpdateDeviceTreeError(ILogger logger, string exceptionType, string message, string stacktrace);

    [LoggerMessage(LogLevel.Error, "An error while trying to save DeviceTree (SaveButtonAsync): {exceptionType} {message} {stacktrace}")]
    public static partial void LogWhileSaveDeviceTreeError(ILogger logger, string exceptionType, string message, string stacktrace);
}
