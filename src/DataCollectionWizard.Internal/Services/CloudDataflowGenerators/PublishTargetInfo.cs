using Sdk.Connections.Contracts;
using ViciOne.Driver.IoTCore.Contracts.DeviceTree;

namespace DataCollectionWizard.Internal.Services.CloudDataflowGenerators;

public sealed record PublishTargetInfo(Connection Connection, ConnectionKind Kind, IReadOnlyCollection<Type> TreeNodesSupportedForConfiguration)
{
    public bool IsSupportedForConfiguration(IDeviceTreeBase node)
        => TreeNodesSupportedForConfiguration.Any(t => node.GetType().IsAssignableTo(t));
}
