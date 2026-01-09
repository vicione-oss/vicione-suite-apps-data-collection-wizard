using ViciOne.Cluster.Builder;
using ViciOne.Cluster.Model;

namespace DataCollectionWizard.Backend.Services;

public interface IClusterService
{
    Task<Guid> RequestUpdateAsync(CancellationToken cancellationToken, Guid? correlationId = null, TimeSpan? validity = null);
    Task<ClusterBuilder> LoadLatestClusterBuilder();
    Task DeployClusterAsync(Guid ticketId, Cluster cluster, CancellationToken? cancellationToken = null);
    void DiscardUpdateRequest(Guid ticketId);
    bool IsTicketValid(Guid ticketId);
}
