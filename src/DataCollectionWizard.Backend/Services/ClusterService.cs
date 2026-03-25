using ClusterManagement.Public;
using ClusterManagement.Public.Commands;
using ClusterManagement.Public.Designs;
using ClusterManagement.Public.Requests;
using DataCollectionWizard.Internal.Events;
using Microsoft.Extensions.Logging;
using Sdk.Backend.Messaging;
using Sdk.Messaging;
using ViciOne.Cluster.Builder;
using ViciOne.Cluster.Model;

namespace DataCollectionWizard.Backend.Services;

public sealed partial class ClusterService(
    IDesignProvider designProvider,
    ISuiteMediator mediator,
    ClusterServiceState state,
    ILogger<ClusterService> logger)
    : IClusterService
{
    private static readonly TimeSpan s_maxTicketValidity = TimeSpan.FromMinutes(1);

    public async Task<Guid> RequestUpdateAsync(CancellationToken cancellationToken, Guid? correlationId = null, TimeSpan? validity = null)
    {
        if (validity > s_maxTicketValidity)
            throw new ArgumentOutOfRangeException(nameof(validity));

        await state.Semaphore.WaitAsync(cancellationToken);
        var ticketId = Guid.NewGuid();
        state.IssuedTicket = new Ticket(ticketId, correlationId ?? ticketId);
        LogIssuedTicket(ticketId);

        state.StartTimer(validity);
        return ticketId;
    }

    public async Task<ClusterBuilder> LoadLatestClusterBuilder()
    {
        // we could have multiple cluster - actually it's only one
        var clusterVersion = (await QueryVersions()).First();

        // maybe that's not useful here because you might want to get the active version but for now...
        var clusterJson = await Query(clusterVersion.Key, clusterVersion.Value.Max(k => k));

        // needed to resolve the cluster dependencies to fbs, dataports etc.
        var resolver = await designProvider.CreateResolver();

        return new ClusterBuilder(resolver);
    }

    public async Task DeployClusterAsync(Guid ticketId,
        Cluster cluster,
        CancellationToken? cancellationToken = null)
    {
        if (state.IssuedTicket is null)
            throw new InvalidOperationException($"Ticket '{ticketId}' is invalid or expired");

        if (!IsTicketValid(ticketId))
            throw new InvalidOperationException($"'{ticketId}' is not the last issued ticked ({state.IssuedTicket.Value.Id})");

        var ct = cancellationToken ?? state.Cts.Token;
        var correlationId = state.IssuedTicket.Value.CorrelationId;

        try
        {
            state.StopTimer();

            var command = new DeployCluster(cluster.Id, null)
            {
                Options = new DeployClusterOptions(ClusterSerializer.Compress(cluster)),
                DeletePreviousVersionOnSuccess = true,
                CorrelationId = correlationId
            };
            var taskCompletionSource = new TaskCompletionSource<ErrorInfo?>();
            state.TaskCompletionSourceMap[correlationId] = taskCompletionSource;
            await mediator.Send(command, ct);
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
    {
        LogDiscardingUpdateRequestForTicket(ticketId);
        state.DiscardUpdateRequest(ticketId);
    }

    public bool IsTicketValid(Guid ticketId) => state.IssuedTicket?.Id == ticketId;

    private async Task<List<ClusterInfo>> QueryClusterInfosAsync(Guid? clusterId = null)
    {
        var query = new GetClusterInfosRequest
        {
            ClusterId = clusterId
        };
        var response = await mediator.Request<GetClusterInfosRequest, GetClusterInfosResponse>(query);

        return response.Results;
    }

    private async Task<Cluster> Query(Guid clusterId, Version? version)
    {
        var response = await mediator
            .Request<GetClusterRequest, GetClusterResponse>(new GetClusterRequest(clusterId, version));

        if (response.RequestError is not null)
            throw new InvalidOperationException(response.RequestError.Message);

        if (response.CompressedCluster is null || response.CompressedCluster.Length == 0)
            throw new InvalidOperationException($"Received empty data for cluster {clusterId} v{version}");

        return await ClusterSerializer.Decompress(response.CompressedCluster);
    }

    private async Task<Dictionary<Guid, List<Version>>> QueryVersions()
        => (await QueryClusterInfosAsync())
        .GroupBy(n => n.Id, n => n.Version)
        .ToDictionary(g => g.Key, g => g.ToList());

    [LoggerMessage(LogLevel.Error, "Failed to apply DeviceTree: {Message} {StackTrace}")]
    public static partial void LogApplicationFailedError(ILogger logger, string message, string stackTrace);

    [LoggerMessage(LogLevel.Debug, "Discarding update request for ticket {TicketId}")]
    partial void LogDiscardingUpdateRequestForTicket(Guid ticketId);

    [LoggerMessage(LogLevel.Debug, "Cluster service issued ticket: {TicketId}")]
    partial void LogIssuedTicket(Guid ticketId);

    [LoggerMessage(LogLevel.Debug, "Ticket timer elapsed")]
    partial void LogTicketTimerElapsed();
}
