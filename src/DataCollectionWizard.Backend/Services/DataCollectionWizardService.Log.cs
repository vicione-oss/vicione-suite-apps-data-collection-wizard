using Microsoft.Extensions.Logging;

namespace DataCollectionWizard.Backend.Services;

public sealed partial class DataCollectionWizardService
{
    [LoggerMessage(LogLevel.Warning, "Failed to use existing engine: {engineName} to retrieve DeviceTree (no connetor ids found) - Creating new dataflow")]
    public static partial void LogEngineFail(ILogger logger, string engineName);

    [LoggerMessage(LogLevel.Debug, "{call} loaded cluster.Version={version}")]
    public static partial void LogLoadedCluster(ILogger logger, string call, Version version);

    [LoggerMessage(LogLevel.Information, "Skip Loading latest cluster v.{version}")]
    public static partial void LogSkipClusterLoading(ILogger logger, Version version);

    [LoggerMessage(LogLevel.Information, "Skipping first message - retained message: {address}")]
    public static partial void LogSkipFirstMessageInfo(ILogger logger, string address);

    [LoggerMessage(LogLevel.Warning, "Did not receive second message: {address} - trying to use device from first message.")]
    public static partial void LogTimeoutUsingFirstMessageWarning(ILogger logger, string address);

    [LoggerMessage(LogLevel.Information, "Received second message: {address}")]
    public static partial void LogReceivedSecondMessageInfo(ILogger logger, string address);

    [LoggerMessage(LogLevel.Debug, "{Call} returns tree.Id {TreeId}")]
    public static partial void LogReturnsTreeIdDebug(ILogger logger, string call, string treeId);

    [LoggerMessage(LogLevel.Error, "Failed to request device {address}: {exception}")]
    public static partial void LogRequestError(ILogger logger, string address, string exception);
}
