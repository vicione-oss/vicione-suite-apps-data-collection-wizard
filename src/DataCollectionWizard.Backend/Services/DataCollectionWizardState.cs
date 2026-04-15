using System.Collections.Concurrent;
using DataCollectionWizard.Internal.Contracts;
using ViciOne.Cluster.Builder;

namespace DataCollectionWizard.Backend.Services;

public class DataCollectionWizardState
{
    private readonly Lock _clusterBuilderLockObject = new();
    private readonly Lock _stateLock = new();

    public ClusterBuilder? ClusterBuilder { get { lock (_clusterBuilderLockObject) { return field; } } set { lock (_clusterBuilderLockObject) { field = value; } } }
    public (Guid trackingId, Func<Task> callback) DeployTrackingInfo { get; set; }
    public ConcurrentDictionary<Uri, DeviceConnectorIds> DeviceTreeConnectors { get; } = [];
    public Version? LatestClusterVersion { get { lock (_stateLock) { return field; } } set { lock (_stateLock) { field = value; } } }
    public Version LatestDeployedClusterVersion { get { lock (_stateLock) { return field; } } set { lock (_stateLock) { field = value; } } } = new();
    public string? MachineIdentifier { get { lock (_stateLock) { return field; } } set { lock (_stateLock) { field = value; } } }
    public ConcurrentBag<(Uri url, Guid correlationId)> RequestedDevices { get; } = [];
}
