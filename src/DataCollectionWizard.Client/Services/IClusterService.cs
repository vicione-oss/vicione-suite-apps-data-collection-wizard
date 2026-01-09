using ClusterManagement.Public.Requests;

namespace DataCollectionWizard.Client.Services;

public interface IClusterService
{
    Task<List<ClusterInfo>> QueryClusterInfosAsync(Guid? clusterId = null);
}
