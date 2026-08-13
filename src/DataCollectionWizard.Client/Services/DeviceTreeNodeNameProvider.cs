using ViciOne.DeviceTree.Contracts;

namespace DataCollectionWizard.Client.Services;

public static class DeviceTreeNodeNameProvider
{
    public static string GetTreeDisplayText(IDeviceTreeBase deviceTreeNode)
    {
        // The device-reported alias stands in for the technical name, which the subtitle shows instead.
        if (deviceTreeNode is IDeviceTreeDeviceAliasNode deviceAliasNode)
            return $"{deviceAliasNode.Alias}";

        if (deviceTreeNode is IDeviceTreeUserAliasNode { Alias: var alias } && !string.IsNullOrWhiteSpace(alias))
            return $"{alias} [{deviceTreeNode.Name}]";

        return $"{deviceTreeNode.Name}";
    }
}
