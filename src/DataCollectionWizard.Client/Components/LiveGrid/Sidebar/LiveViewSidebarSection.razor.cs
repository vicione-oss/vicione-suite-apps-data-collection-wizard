using DataCollectionWizard.Client.Components.LiveGrid.Services;
using DataCollectionWizard.Client.Extensions;
using DataCollectionWizard.Client.Models.DeviceTree;
using Microsoft.AspNetCore.Components;
using ViciOne.Driver.IoTCore.Contracts.DeviceTree;
using ViciOne.Ui.TreeEditor.Builder.Interface.Nodes;

namespace DataCollectionWizard.Client.Components.LiveGrid.Sidebar;

public sealed partial class LiveViewSidebarSection : ComponentBase, IDisposable
{
    [CascadingParameter]
    private LiveGridService Service { get; set; } = default!;

    public void Dispose()
        => Service.TreeBuilder.Selection.SelectionChanged -= OnTreeSelectionChangedAsync;

    private void OnFilterTextChanged(string filterText)
    {
        if (string.IsNullOrWhiteSpace(filterText))
            Service.TreeBuilder.Filter.SetFilter([]);
        else
            Service.TreeBuilder.Filter.SetFilter([FilterFunc]);

        bool FilterFunc(ITreeNode node)
        {
            if (node is not NodeBase nodeBase)
                return false;

            var filterResult = nodeBase.GetFilterResult(filterText);

            if (!filterResult && nodeBase.Device is IDeviceTreeDataNode && nodeBase.Parent is not null)
                filterResult = nodeBase.Parent.GetFilterResult(filterText);

            return filterResult;
        }
    }

    private void OnChangeTreeExpansionClicked(bool expand)
    {
        if (Service.TreeBuilder.Selection.SelectedNodes.Count == 0)
        {
            if (expand)
                Service.TreeBuilder.Expansion.ChangeExpansionForLayers(expand);
            else
                Service.TreeBuilder.Expansion.ChangeExpansionForLayers(expand, 1);

            return;
        }

        foreach (var selectedNode in Service.TreeBuilder.Selection.SelectedNodes)
        {
            if (selectedNode is not NodeBase nodeBase)
                continue;

            ExpandChildrenRecursive(nodeBase);
        }

        void ExpandChildrenRecursive(NodeBase node)
        {
            Service.TreeBuilder.Expansion.ChangeExpansion(node, expand);

            foreach (var child in node.Children)
            {
                Service.TreeBuilder.Expansion.ChangeExpansion(child, expand);
                ExpandChildrenRecursive(child);
            }
        }
    }

    protected override void OnInitialized()
        => Service.TreeBuilder.Selection.SelectionChanged += OnTreeSelectionChangedAsync;

    private async void OnTreeSelectionChangedAsync(ITreeNode node, bool selected)
        => await InvokeAsync(StateHasChanged);
}
