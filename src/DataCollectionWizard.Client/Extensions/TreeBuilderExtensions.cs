using DataCollectionWizard.Client.Models.DeviceTree;
using ViciOne.Driver.IoTCore.Contracts.DeviceTree;
using ViciOne.Ui.TreeEditor.Builder;
using ViciOne.Ui.TreeEditor.Builder.Interface.Nodes;

namespace DataCollectionWizard.Client.Extensions;

public static class TreeBuilderExtensions
{
    public static void ApplyFilter(this TreeBuilder treeBuilder, string filterExpression)
    {
        if (string.IsNullOrWhiteSpace(filterExpression))
            treeBuilder.Filter.SetFilter([]);
        else
            treeBuilder.Filter.SetFilter([FilterFunc]);

        bool FilterFunc(ITreeNode node)
        {
            if (node is not NodeBase nodeBase)
                return false;

            var filterResult = nodeBase.GetFilterResult(filterExpression);

            if (!filterResult && nodeBase.Device is IDeviceTreeDataNode && nodeBase.Parent is not null)
                filterResult = nodeBase.Parent.GetFilterResult(filterExpression);

            return filterResult;
        }
    }

    public static void ChangeExpansion(this TreeBuilder treeBuilder, bool expand)
    {
        if (treeBuilder.Selection.SelectedNodes.Count == 0)
        {
            if (expand)
                treeBuilder.Expansion.ChangeExpansionForLayers(true);
            else
                treeBuilder.Expansion.ChangeExpansionForLayers(false, 1);

            return;
        }

        foreach (var selectedNode in treeBuilder.Selection.SelectedNodes)
        {
            if (selectedNode is not NodeBase nodeBase)
                continue;

            ExpandChildrenRecursive(nodeBase);
        }

        void ExpandChildrenRecursive(NodeBase node)
        {
            treeBuilder.Expansion.ChangeExpansion(node, expand);

            foreach (var child in node.Children)
            {
                treeBuilder.Expansion.ChangeExpansion(child, expand);
                ExpandChildrenRecursive(child);
            }
        }
    }
}
