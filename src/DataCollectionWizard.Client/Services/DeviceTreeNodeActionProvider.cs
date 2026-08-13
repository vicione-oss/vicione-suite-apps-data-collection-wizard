using ViciOne.DeviceTree.Contracts;

namespace DataCollectionWizard.Client.Services;

internal static class DeviceTreeNodeActionProvider
{
    public static bool IsDeletable(IDeviceTreeBase deviceTreeNode)
        => deviceTreeNode is IDeviceTreeMasterNode || deviceTreeNode.Status != ConnectionStatus.Online;

    public static bool IsEditable(IDeviceTreeBase deviceTreeBase)
        => deviceTreeBase is IDeviceTreeUserAliasNode;
}
