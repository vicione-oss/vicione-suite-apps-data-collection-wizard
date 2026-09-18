using Microsoft.Extensions.Logging;

namespace DataCollectionWizard.Backend.Consumers;

public sealed partial class DataCollectionWizardConsumer
{
    [LoggerMessage(LogLevel.Warning, "Failed to commit cluster {ClusterVersion}({ClusterId}): {Error}({ErrorCode})")]
    private static partial void LogClusterCommitFailed(ILogger logger, Version clusterVersion, Guid clusterId, string? error, int errorCode);

    [LoggerMessage(LogLevel.Warning, "Failed to update cluster {ClusterVersion}({ClusterId}): {Errors}")]
    private static partial void LogClusterUpdateFailed(ILogger logger, Version clusterVersion, Guid clusterId, string errors);

    [LoggerMessage(LogLevel.Debug, "DCW received cluster update completed {ClusterVersion} {CorrelationId}")]
    private static partial void LogDcwReceivedClusterUpdateCompleted(ILogger<DataCollectionWizardConsumer> logger, Version ClusterVersion, Guid? CorrelationId);

    [LoggerMessage(LogLevel.Debug, "DCW received cluster changed {ClusterVersion} {CorrelationId}")]
    private static partial void LogDcwReceivedClusterChanged(ILogger<DataCollectionWizardConsumer> logger, Version? ClusterVersion, Guid? CorrelationId);

    [LoggerMessage(LogLevel.Debug, "DCW received cluster not deleted")]
    private static partial void LogDcwReceivedClusterNotDeleted(ILogger<DataCollectionWizardConsumer> logger);

    [LoggerMessage(LogLevel.Debug, "Attempt to update cluster {ClusterVersion}({ClusterId}) was rejected")]
    private static partial void LogClusterUpdateRejected(ILogger<DataCollectionWizardConsumer> logger, Version? ClusterVersion, Guid? ClusterId);
}
