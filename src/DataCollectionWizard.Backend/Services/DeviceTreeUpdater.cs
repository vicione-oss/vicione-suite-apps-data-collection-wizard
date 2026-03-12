using DataCollectionWizard.Internal.Commands;
using DataCollectionWizard.Internal.Events;
using DataCollectionWizard.Public.Services;
using Microsoft.Extensions.Logging;
using Sdk.Backend.Messaging;
using Sdk.Messaging;
using ViciOne.Driver.IoTCore.Contracts.DeviceTree;

namespace DataCollectionWizard.Backend.Services;

public sealed partial class DeviceTreeUpdater(
    ISuiteMediator mediator,
    IDataCollectionWizardService dcwService,
    IClusterService clusterService,
    DeviceTreeUpdaterState state,
    ClusterServiceState clusterServiceState,
    ILogger<DeviceTreeUpdater> logger)
    : IDeviceTreeUpdater
{
    private const int CommandTimeoutMs = 60_000;

    public Task<Guid> RequestUpdateAsync(CancellationToken cancellationToken, Guid? correlationId = null, TimeSpan? validity = null)
        => clusterService.RequestUpdateAsync(cancellationToken, correlationId, validity);

    public Task<DeviceTreeRoot> LoadDeviceTree(CancellationToken? cancellationToken = null)
    {
        var ct = cancellationToken ?? state.Cts.Token;
        return dcwService.RequestDeviceTreeAsync(ct);
    }

    public async Task UpdateDeviceTreeAsync(Guid ticketId,
        DeviceTreeRoot deviceTree,
        IDeviceTreeBase[] deletedNodes,
        IEnumerable<string> masterNodesToUpdate,
        LogLevel? logLevel = null,
        bool saveTree = true,
        CancellationToken? cancellationToken = null)
    {
        if (clusterServiceState.IssuedTicket is null)
            throw new InvalidOperationException($"Ticket '{ticketId}' is invalid or expired");

        if (!clusterService.IsTicketValid(ticketId))
            throw new InvalidOperationException($"'{ticketId}' is not the last issued ticked ({clusterServiceState.IssuedTicket.Value.Id})");

        var ct = cancellationToken ?? state.Cts.Token;
        var correlationId = clusterServiceState.IssuedTicket.Value.CorrelationId;
        try
        {
            clusterServiceState.StopTimer();

            if (saveTree)
            {
                var command = new SaveDeviceTree(deviceTree) { CorrelationId = correlationId };
                var taskCompletionSource = new TaskCompletionSource<ErrorInfo?>();
                state.TaskCompletionSourceMap[correlationId] = taskCompletionSource;

                try
                {
                    await mediator.Send(command, ct);

                    if (await WaitForCommandCompletion(taskCompletionSource, ct) is { } error)
                    {
                        await mediator.Publish(new DeviceTreeApplicationEvent(error) { CorrelationId = correlationId }, ct);
                        return;
                    }
                }
                finally
                {
                    state.TaskCompletionSourceMap.TryRemove(correlationId, out _);
                }
            }

            var cluster = await dcwService.ApplyDeviceTreeAsync(masterNodesToUpdate, deletedNodes, deviceTree, logLevel, ct);
            await clusterService.DeployClusterAsync(ticketId, cluster, ct);
        }
        catch (OperationCanceledException)
        {
            DiscardUpdateRequest(ticketId);
            if (cancellationToken is not null) //external cancellation
                throw;
        }
        catch (ObjectDisposedException)
        {
            DiscardUpdateRequest(ticketId);
            // Semaphore already disposed, nothing we can do, return gracefully
        }
        catch (Exception ex)
        {
            DiscardUpdateRequest(ticketId);
            await mediator.Publish(new DeviceTreeApplicationEvent(new ErrorInfo(-1, ex.Message)) { CorrelationId = correlationId }, ct);
            LogApplicationFailedError(logger, ex.Message, ex.StackTrace ?? string.Empty);
        }
    }

    public void DiscardUpdateRequest(Guid ticketId)
        => clusterService.DiscardUpdateRequest(ticketId);

    private static async Task<ErrorInfo?> WaitForCommandCompletion(TaskCompletionSource<ErrorInfo?> taskCompletionSource,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var errorInfo =
                await taskCompletionSource.Task.WaitAsync(TimeSpan.FromMilliseconds(CommandTimeoutMs),
                    cancellationToken);
            return errorInfo;
        }
        catch (TimeoutException)
        {
            return new ErrorInfo(0, "timeout");
        }
    }

    [LoggerMessage(LogLevel.Error, "Failed to apply DeviceTree: {message} {stackTrace}")]
    public static partial void LogApplicationFailedError(ILogger logger, string message, string stackTrace);
}
