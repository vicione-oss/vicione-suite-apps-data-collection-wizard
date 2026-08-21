using ClusterManagement.Public.Connections.Extensions;
using DataCollectionWizard.Public;
using Sdk.Connections.Contracts;
using ViciOne.DeviceTree.Contracts;

namespace DataCollectionWizard.Internal.Services.CloudDataflowGenerators;

public class OpcUaCloudFilter : ICloudFilter
{
    public Type CloudDataflowGeneratorType => typeof(OpcUaCloudDataflowGenerator);

    public IReadOnlyCollection<Type> TreeNodesSupportedForConfiguration => [typeof(IDeviceTreeCompressableDataNode),];

    public ConnectionKind ConnectionKind => ConnectionKind.OpcUa;

    public IEnumerable<Connection> GetCloudConnections(IEnumerable<Connection> connections)
        => [.. connections.Where(k => k.Type == ConnectionType.OpcUaServer)];
}
