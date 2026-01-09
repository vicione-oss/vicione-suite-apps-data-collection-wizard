using DataCollectionWizard.Client.Components.LiveGrid.Services;
using DataCollectionWizard.Client.Models.DeviceTree;
using Microsoft.AspNetCore.Components;
using ViciOne.Driver.IoTCore.Contracts.DeviceTree;
using ViciOne.Ui.TreeEditor.Builder.Interface;

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
            var filterResult = node.DisplayText.Contains(filterText, StringComparison.InvariantCultureIgnoreCase);

            if (node is NodeBase nodeBase)
            {
                filterResult |= !string.IsNullOrWhiteSpace(nodeBase.Subtitle) && nodeBase.Subtitle.Contains(filterText, StringComparison.InvariantCultureIgnoreCase);

                if (!filterResult && nodeBase.Device is IDeviceTreeDataNode && nodeBase.Parent is not null)
                {
                    filterResult = nodeBase.Parent.DisplayText.Contains(filterText, StringComparison.InvariantCultureIgnoreCase);
                    filterResult |= !string.IsNullOrWhiteSpace(nodeBase.Parent.Subtitle) && nodeBase.Parent.Subtitle.Contains(filterText, StringComparison.InvariantCultureIgnoreCase);
                }
            }

            return filterResult;
        }
    }

    private void OnChangeTreeExpansionClicked(bool expand)
    {
        if (!Service.TreeBuilder.Selection.SelectedNodes.Any())
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

    private async void OnTreeSelectionChangedAsync(ITreeNode node)
        => await InvokeAsync(StateHasChanged);
}
