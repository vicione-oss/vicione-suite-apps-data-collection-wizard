using DataCollectionWizard.Backend.DbContext;
using DataCollectionWizard.Backend.Extensions;
using DataCollectionWizard.Internal.Events;
using DataCollectionWizard.Public;
using DataCollectionWizard.Public.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sdk.Backend.Messaging;
using Sdk.Messaging;
using ViciOne.Driver.IoTCore.Contracts.DeviceTree;

namespace DataCollectionWizard.Backend.Services;

public sealed partial class DeviceTreeUpdater(
    ISuiteMediator mediator,
    IDataCollectionWizardDbContext dbContext,
    IDataCollectionWizardService dcwService,
    IClusterService clusterService,
    DeviceTreeUpdaterState state,
    ClusterServiceState clusterServiceState,
    ILogger<DeviceTreeUpdater> logger)
    : IDeviceTreeUpdater
{

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
        var correlationId = clusterServiceState.ValidateTicketAndStopTimer(ticketId);
        var ct = cancellationToken ?? state.Cts.Token;
        try
        {
            if (saveTree)
            {
                dbContext.UpsertDeviceTree(deviceTree);

                try
                {
                    if (await dbContext.SaveChangesAsync(ct) > 0)
                    {
                        await mediator.Publish(new DeviceTreeChangedEvent(CrudAction.Updated), ct);
                    }
                }
                catch (DbUpdateException e)
                {
                    var error = new ErrorInfo(ErrorCodes.DbUpdateFailed, e.Message);
                    await mediator.Publish(new DeviceTreeChangeErrorEvent(error), CancellationToken.None);
                    DiscardUpdateRequest(ticketId);
                    await mediator.Publish(new DeviceTreeApplicationEvent(error) { CorrelationId = correlationId }, CancellationToken.None);
                    return;
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
            await mediator.Publish(new DeviceTreeApplicationEvent(new ErrorInfo(-1, ex.Message)) { CorrelationId = correlationId }, CancellationToken.None);
            LogApplicationFailedError(logger, ex.Message, ex.StackTrace ?? string.Empty);
        }
    }

    public void DiscardUpdateRequest(Guid ticketId)
        => clusterService.DiscardUpdateRequest(ticketId);

    [LoggerMessage(LogLevel.Error, "Failed to apply DeviceTree: {message} {stackTrace}")]
    public static partial void LogApplicationFailedError(ILogger logger, string message, string stackTrace);
}
