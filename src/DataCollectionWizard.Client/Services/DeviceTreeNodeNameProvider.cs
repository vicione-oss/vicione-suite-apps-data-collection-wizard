using ViciOne.Driver.IoTCore.Contracts.DeviceTree;

namespace DataCollectionWizard.Client.Services;

public static class DeviceTreeNodeNameProvider
{
    public static string GetTreeDisplayText(IDeviceTreeBase deviceTreeNode)
    {
        if (deviceTreeNode is IDeviceTreeAliasNode aliasNode && !string.IsNullOrWhiteSpace(aliasNode.NameAlias))
            return $"{aliasNode.NameAlias} [{deviceTreeNode.Name}]";

        if (deviceTreeNode is IAliasStructureNode aliasStructureNode)
            return $"{aliasStructureNode.Alias}";

        return $"{deviceTreeNode.Name}";
    }
}
