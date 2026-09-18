using DataCollectionWizard.Public;
using Sdk.Connections.Contracts;
using ViciOne.DeviceTree.Contracts;

namespace DataCollectionWizard.Internal.Services.CloudDataflowGenerators;

public sealed class AnnaCloudFilter : ICloudFilter
{
    public Type CloudDataflowGeneratorType => typeof(AnnaCloudDataflowGenerator);

    public IReadOnlyCollection<Type> TreeNodesSupportedForConfiguration => [
        typeof(IDeviceTreeCompressableDataNode),
        typeof(IDeviceTreeConfigurableRawDataNode),
        typeof(IDeviceTreeEventTriggerDataNode),
        typeof(IDeviceTreeSchedulableDataNode),
        ];

    public ConnectionKind ConnectionKind => ConnectionKind.Anna;

    public IEnumerable<Connection> GetCloudConnections(IEnumerable<Connection> connections)
        => [.. connections.Where(IsAnnaConnection)];

    public static bool IsAnnaConnection(Connection connection)
        => connection.Tags.Contains(Constants.AnnaCloud) && connection.Type == ConnectionType.Http;
}
