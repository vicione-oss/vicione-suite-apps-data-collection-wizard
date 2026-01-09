using ViciOne.Driver.IoTCore.Contracts.DeviceTree;

namespace DataCollectionWizard.Client.Services;

internal static class DeviceTreeNodeActionProvider
{
    public static bool IsConfigurable(IDeviceTreeBase _)
        => false;

    public static bool IsDeletable(IDeviceTreeBase deviceTreeNode)
        => deviceTreeNode is IDeviceTreeMasterNode || deviceTreeNode.IsOffline;

    public static bool IsEditable(IDeviceTreeBase deviceTreeBase)
        => deviceTreeBase is IDeviceTreeAliasNode;
}
