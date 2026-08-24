using ViciOne.DeviceTree.Contracts;

namespace DataCollectionWizard.Client.Services;

public static class DeviceTreeNodeNameProvider
{
    public static string GetTreeDisplayText(IDeviceTreeBase deviceTreeNode)
    {
        // The device-reported alias stands in for the technical name, which the subtitle shows instead.
        if (deviceTreeNode is IDeviceTreeDeviceAliasNode deviceAliasNode)
            return $"{deviceAliasNode.Alias}";

        if (deviceTreeNode is IDeviceTreeUserAliasNode { Alias: var alias })
            return GetUserAliasDisplayText(alias, deviceTreeNode.Name);

        return $"{deviceTreeNode.Name}";
    }

    /// <summary>
    /// How a node with a user-entered alias appears in the tree: the alias followed by the technical name in
    /// brackets, or the name alone when no alias is set.
    /// </summary>
    /// <remarks>
    /// Separate from <see cref="GetTreeDisplayText"/> so the alias dialog can preview an alias that has not been
    /// stored on the node yet, without spelling the format out a second time.
    /// </remarks>
    /// <param name="alias">The alias to show, which may be empty.</param>
    /// <param name="name">The node's own technical name.</param>
    public static string GetUserAliasDisplayText(string? alias, string name)
        => string.IsNullOrWhiteSpace(alias) ? name : $"{alias.Trim()} [{name}]";
}
