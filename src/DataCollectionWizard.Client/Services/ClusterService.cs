using ClusterManagement.Public.Requests;
using Sdk.Client.Infrastructure;

namespace DataCollectionWizard.Client.Services;

public sealed class ClusterService(IUiMediator mediator) : IClusterService
{
    public async Task<List<ClusterInfo>> QueryClusterInfosAsync(Guid? clusterId = null)
    {
        var query = new GetClusterInfosRequest
        {
            ClusterId = clusterId
        };
        var response = await mediator.Request<GetClusterInfosRequest, GetClusterInfosResponse>(query);

        return response.Results;
    }
}
