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

    public async Task Consume(ConsumeContext<ClusterUpdateFailed> context)
    {
        var errors = string.Join(" | ", context.Message.ErrorsByApplication.Select(kvp => $"ApplicationId:{kvp.Key} {kvp.Value.Message} ({kvp.Value.ErrorCode})"));
        if (!clusterServiceState.SetResult(context.Message.CorrelationId, new ErrorInfo(-1, errors)))
            return;

        await context.Publish(new DeviceTreeApplicationEvent(new ErrorInfo(-1, errors)) { CorrelationId = context.Message.CorrelationId }, context.CancellationToken);
        LogClusterUpdateFailed(logger, context.Message.Version, context.Message.ClusterId, errors);
    }

    public async Task Consume(ConsumeContext<CommitClusterChangeFailed> context)
    {
        var error = new ErrorInfo(context.Message.Error.ErrorCode, context.Message.Error.Message);
        if (!clusterServiceState.SetResult(context.Message.CorrelationId, error))
            return;

        await context.Publish(new DeviceTreeApplicationEvent(error) { CorrelationId = context.Message.CorrelationId }, context.CancellationToken);
        LogClusterCommitFailed(logger,
            context.Message.Version,
            context.Message.ClusterId,
            context.Message.Error.Message,
            context.Message.Error.ErrorCode);
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

    public async Task Consume(ConsumeContext<ClusterUpdateRejected> context)
    {
        if (!clusterServiceState.SetResult(context.Message.CorrelationId, new ErrorInfo(-1, "Cluster update was rejected")))
            return;

        await context.Publish(new DeviceTreeApplicationEvent(new ErrorInfo(-1, "Cluster update was rejected")) { CorrelationId = context.Message.CorrelationId }, context.CancellationToken);
        LogClusterUpdateRejected(logger, context.Message.RejectedVersion, context.Message.ClusterId);
    }
}
