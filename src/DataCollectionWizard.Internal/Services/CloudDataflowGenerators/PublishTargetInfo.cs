using Sdk.Connections.Contracts;
using ViciOne.DeviceTree.Contracts;

namespace DataCollectionWizard.Internal.Services.CloudDataflowGenerators;

public sealed record PublishTargetInfo(Connection Connection, ConnectionKind Kind, IReadOnlyCollection<Type> TreeNodesSupportedForConfiguration)
{
    public bool IsSupportedForConfiguration(IDeviceTreeBase? node)
        => node is not null && TreeNodesSupportedForConfiguration.Any(t => node.GetType().IsAssignableTo(t));
}
