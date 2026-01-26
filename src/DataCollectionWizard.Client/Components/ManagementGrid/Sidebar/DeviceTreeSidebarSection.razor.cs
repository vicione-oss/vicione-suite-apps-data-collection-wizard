using System.Drawing;
using DataCollectionWizard.Client.Components.ManagementGrid.Services;
using DataCollectionWizard.Client.Extensions;
using DataCollectionWizard.Client.Models.DeviceTree;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using ViciOne.Driver.IoTCore.Contracts.DeviceTree;
using ViciOne.Ui.TreeEditor.Builder.Interface.Nodes;

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
            if (node is not NodeBase nodeBase)
                return false;

            var filterResult = nodeBase.GetFilterResult(filterText);

            if (!filterResult && nodeBase.Device is IDeviceTreeDataNode && nodeBase.Parent is not null)
                filterResult = nodeBase.Parent.GetFilterResult(filterText);

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

    private async void OnTreeSelectionChangedAsync(ITreeNode node, bool selected)
        => await InvokeAsync(StateHasChanged);

    private void ServicePropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(Service.DisableClusterActions))
        {
            InvokeAsync(StateHasChanged);
        }
    }
}
