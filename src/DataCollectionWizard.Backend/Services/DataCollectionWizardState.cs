using System.Collections.Concurrent;
using DataCollectionWizard.Internal.Contracts;
using ViciOne.Cluster.Builder;

namespace DataCollectionWizard.Backend.Services;

public class DataCollectionWizardState
{
    private readonly Lock _clusterBuilderLockObject = new();
    private ClusterBuilder? _clusterBuilder;

    public ClusterBuilder? ClusterBuilder { get { lock (_clusterBuilderLockObject) { return _clusterBuilder; } } set { lock (_clusterBuilderLockObject) { _clusterBuilder = value; } } }
    public (Guid trackingId, Func<Task> callback) DeployTrackingInfo { get; set; }
    public Dictionary<Uri, DeviceConnectorIds> DeviceTreeConnectors { get; } = [];
    public Version? LatestClusterVersion { get; set; }
    public Version LatestDeployedClusterVersion { get; set; } = new();
    public string? MachineIdentifier { get; set; }
    public ConcurrentBag<(Uri url, Guid correlationId)> RequestedDevices { get; } = [];
}
