using ClusterManagement.Public.Events;
using DataCollectionWizard.Backend.Services;
using DataCollectionWizard.Internal.Events;
using MassTransit;
using Microsoft.Extensions.Logging;
using Sdk.Messaging;

namespace DataCollectionWizard.Backend.Consumers;

public sealed partial class DataCollectionWizardConsumer(DataCollectionWizardState dataCollectionWizardState,
    ClusterServiceState clusterServiceState,
    IDeviceTreeGuard deviceTreeGuard,
    ILogger<DataCollectionWizardConsumer> logger)
    : IConsumer<ClusterChanged>,
       IConsumer<ClusterUpdateCompleted>,
       IConsumer<ClusterUpdateFailed>,
       IConsumer<CommitClusterChangeFailed>,
       IConsumer<DeviceConnectorIdsChangedEvent>,
       IConsumer<DeviceTreeApplicationEvent>,
       IConsumer<ClusterUpdateRejected>
{
    public Task Consume(ConsumeContext<ClusterChanged> context)
    {
        LogDcwReceivedClusterChanged(logger, context.Message.Version, context.CorrelationId);

        // cluster version was deleted
        if (context.Message.Action == CrudAction.Deleted)
        {
            if (context.Message.Version == dataCollectionWizardState.LatestClusterVersion)
            {
                dataCollectionWizardState.ClusterBuilder?.Dispose();
                dataCollectionWizardState.ClusterBuilder = null;
                dataCollectionWizardState.LatestClusterVersion = null;
            }

            return Task.CompletedTask;
        }

        LogDcwReceivedClusterNotDeleted(logger);

        // after cluster was changed this is the latest version
        // if this version does not match our builder version we could inform the user to trigger a refresh
        dataCollectionWizardState.LatestClusterVersion = context.Message.Version;
        return Task.CompletedTask;
    }

    public async Task Consume(ConsumeContext<ClusterUpdateCompleted> context)
    {
        if (!clusterServiceState.SetResult(context.Message.CorrelationId))
            return;

        await context.Publish(new DeviceTreeApplicationEvent { CorrelationId = context.Message.CorrelationId }, context.CancellationToken);
        LogDcwReceivedClusterUpdateCompleted(logger, context.Message.NewVersion, context.CorrelationId);

        dataCollectionWizardState.LatestDeployedClusterVersion = context.Message.NewVersion;
        while (dataCollectionWizardState.RequestedDevices.TryTake(out var requestedDevice))
        {
            await context.Publish(new DeviceTreeEngineAddedEvent
            {
                Address = requestedDevice.url,
                CorrelationId = requestedDevice.correlationId,
                DeviceTreeConnectors = dataCollectionWizardState.DeviceTreeConnectors[requestedDevice.url],
            });
        }
    }

    public Task Consume(ConsumeContext<ClusterUpdateFailed> context)
    {
        var errors = string.Join(" | ", context.Message.ErrorsByApplication.Select(kvp => $"ApplicationId:{kvp.Key} {kvp.Value.Message} ({kvp.Value.ErrorCode})"));
        if (!clusterServiceState.SetResult(context.CorrelationId, new ErrorInfo(-1, errors)))
            return Task.CompletedTask;

        LogClusterUpdateFailed(logger, context.Message.Version, context.Message.ClusterId, errors);
        return Task.CompletedTask;
    }

    public Task Consume(ConsumeContext<CommitClusterChangeFailed> context)
    {
        if (dataCollectionWizardState.ClusterBuilder?.Cluster.Id != context.Message.ClusterId)
            return Task.CompletedTask;

        LogClusterCommitFailed(logger,
            context.Message.Version,
            context.Message.ClusterId,
            context.Message.Error.Message,
            context.Message.Error.ErrorCode);
        return Task.CompletedTask;
    }

    public Task Consume(ConsumeContext<DeviceConnectorIdsChangedEvent> context)
        => deviceTreeGuard.OnDeviceConnectorIdsChanged(context.Message.ChangedItems);

    public async Task Consume(ConsumeContext<DeviceTreeApplicationEvent> context)
    {
        if (context.Message.WasSuccessful)
        {
            await deviceTreeGuard.OnDeviceTreeApplication();
        }
    }

    public Task Consume(ConsumeContext<ClusterUpdateRejected> context)
    {
        if (!clusterServiceState.SetResult(context.CorrelationId, new ErrorInfo(-1, "Cluster update was rejected")))
            return Task.CompletedTask;

        LogClusterUpdateRejected(logger, context.Message.RejectedVersion, context.Message.ClusterId);
        return Task.CompletedTask;
    }

    [LoggerMessage(LogLevel.Warning, "Failed to commit cluster {ClusterVersion}({ClusterId}): {Error}({ErrorCode})")]
    static partial void LogClusterCommitFailed(ILogger logger, Version clusterVersion, Guid clusterId, string? error, int errorCode);

    [LoggerMessage(LogLevel.Warning, "Failed to update cluster {ClusterVersion}({ClusterId}): {Errors}")]
    static partial void LogClusterUpdateFailed(ILogger logger, Version clusterVersion, Guid clusterId, string errors);

    [LoggerMessage(LogLevel.Debug, "DCW received cluster update completed {ClusterVersion} {CorrelationId}")]
    static partial void LogDcwReceivedClusterUpdateCompleted(ILogger<DataCollectionWizardConsumer> logger, Version ClusterVersion, Guid? CorrelationId);

    [LoggerMessage(LogLevel.Debug, "DCW received cluster changed {ClusterVersion} {CorrelationId}")]
    static partial void LogDcwReceivedClusterChanged(ILogger<DataCollectionWizardConsumer> logger, Version? ClusterVersion, Guid? CorrelationId);

    [LoggerMessage(LogLevel.Debug, "DCW received cluster not deleted")]
    static partial void LogDcwReceivedClusterNotDeleted(ILogger<DataCollectionWizardConsumer> logger);

    [LoggerMessage(LogLevel.Debug, "Attempt to update cluster {ClusterVersion}({ClusterId}) was rejected")]
    static partial void LogClusterUpdateRejected(ILogger<DataCollectionWizardConsumer> logger, Version? ClusterVersion, Guid? ClusterId);
}
