using Microsoft.Extensions.Logging;

namespace DataCollectionWizard.Backend.Services;

public sealed partial class DataCollectionWizardService
{
    [LoggerMessage(LogLevel.Warning, "Failed to use existing engine {engineName} to retrieve DeviceTree (no connector ids found), creating new dataflow instead")]
    private static partial void LogEngineFail(ILogger logger, string engineName);

    [LoggerMessage(LogLevel.Debug, "{call} loaded cluster version {version}")]
    private static partial void LogLoadedCluster(ILogger logger, string call, Version version);

    [LoggerMessage(LogLevel.Information, "Skipping loading of latest cluster v.{version}")]
    private static partial void LogSkipClusterLoading(ILogger logger, Version version);

    [LoggerMessage(LogLevel.Information, "Skipping first message - retained message: {address}")]
    private static partial void LogSkipFirstMessageInfo(ILogger logger, string address);

    [LoggerMessage(LogLevel.Warning, "Did not receive second message: {address} - trying to use device from first message.")]
    private static partial void LogTimeoutUsingFirstMessageWarning(ILogger logger, string address);

    [LoggerMessage(LogLevel.Information, "Received second message: {address}")]
    private static partial void LogReceivedSecondMessageInfo(ILogger logger, string address);

    [LoggerMessage(LogLevel.Debug, "{call} returns tree id {treeId}")]
    private static partial void LogReturnsTreeIdDebug(ILogger logger, string call, string treeId);

    [LoggerMessage(LogLevel.Error, "Failed to request device {address}")]
    private static partial void LogRequestError(ILogger logger, string address, Exception exception);
}
