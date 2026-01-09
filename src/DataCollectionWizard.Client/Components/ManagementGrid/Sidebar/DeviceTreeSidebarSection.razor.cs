using System.Drawing;
using DataCollectionWizard.Client.Components.ManagementGrid.Services;
using DataCollectionWizard.Client.Models.DeviceTree;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using ViciOne.Driver.IoTCore.Contracts.DeviceTree;
using ViciOne.Ui.TreeEditor.Builder.Interface;

namespace DataCollectionWizard.Client.Components.ManagementGrid.Sidebar;

public sealed partial class DeviceTreeSidebarSection : ComponentBase, IDisposable
{
    private Point _addDeviceMenuRootPosition = Point.Empty;
    private bool _displayAddDeviceMenu;

    [CascadingParameter]
    private ManagementGridService Service { get; set; } = default!;

    private void CloseAddDeviceMenu()
    {
        _displayAddDeviceMenu = false;
        _addDeviceMenuRootPosition = Point.Empty;
    }

    public void Dispose()
    {
        Service.PropertyChanged -= ServicePropertyChanged;
        Service.TreeBuilder.Selection.SelectionChanged -= OnTreeSelectionChangedAsync;
    }

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

    private void OnAddDeviceClicked()
        => _displayAddDeviceMenu = true;

    private void OnAddDeviceMenuPointerLeave()
        => CloseAddDeviceMenu();

    private void OnAddIoLinkMasterClicked()
    {
        CloseAddDeviceMenu();
        Service.ReqestAddIoLinkMaster();
    }

    private void OnAddVseClicked()
    {
        CloseAddDeviceMenu();
        Service.RequestAddNewVse();
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

    private void OnContainerClicked(MouseEventArgs e)
    {
        if (!_displayAddDeviceMenu)
        {
            _addDeviceMenuRootPosition = Point.Empty;
            return;
        }

        _addDeviceMenuRootPosition = new Point((int)e.ClientX - 5, (int)e.ClientY - 5);
    }

    protected override void OnInitialized()
    {
        Service.TreeBuilder.Selection.SelectionChanged += OnTreeSelectionChangedAsync;
        Service.PropertyChanged += ServicePropertyChanged;

        base.OnInitialized();
    }

    private async void OnTreeSelectionChangedAsync(ITreeNode node)
        => await InvokeAsync(StateHasChanged);

    private void ServicePropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(Service.DisableClusterActions))
        {
            InvokeAsync(StateHasChanged);
        }
    }
}
