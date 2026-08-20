using DataCollectionWizard.Public;
using Sdk.Connections.Contracts;
using ViciOne.DeviceTree.Contracts;

namespace DataCollectionWizard.Internal.Services.CloudDataflowGenerators;

public class MqttCloudFilter : ICloudFilter
{
    public Type CloudDataflowGeneratorType => typeof(MqttCloudDataflowGenerator);

    public IReadOnlyCollection<Type> TreeNodesSupportedForConfiguration =>  [typeof(IDeviceTreeCompressableDataNode),];

    public ConnectionKind ConnectionKind => ConnectionKind.Mqtt;

    public IEnumerable<Connection> GetCloudConnections(IEnumerable<Connection> connections)
        => [.. connections.Where(k => k.Type == ConnectionType.Mqtt && !k.Managed && !k.Tags.Contains(Constants.MoneoConnectCloud))];
}
