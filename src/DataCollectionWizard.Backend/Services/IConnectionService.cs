using Sdk.Connections.Contracts;

namespace DataCollectionWizard.Backend.Services;

public interface IConnectionService
{
    Task<List<Connection>> GetConnectionsAsync(CancellationToken cancellationToken);
}
