using Sdk.Backend.Messaging;
using Sdk.Connections.Contracts;
using Sdk.Connections.Requests;

namespace DataCollectionWizard.Backend.Services;

public class ConnectionService(ISuiteMediator mediator) : IConnectionService
{
    public async Task<List<Connection>> GetConnectionsAsync(CancellationToken cancellationToken)
    {
        var request = new GetConnections(null, null);
        var response = await mediator.Request<GetConnections, GetConnectionsResponse>(request, cancellationToken);
        return response.Connections;
    }
}
