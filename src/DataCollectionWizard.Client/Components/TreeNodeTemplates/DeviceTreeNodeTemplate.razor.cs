using DataCollectionWizard.Client.Models.DeviceTree;
using Microsoft.AspNetCore.Components.Web;
using ViciOne.Ui.TreeEditor.Builder.Interface.Enums;
using ViciOne.Ui.TreeEditor.Builder.Interface.NodeActions;
using ViciOne.Ui.TreeEditor.Templates;
using ViciOne.Ui.TreeEditor.Templates.Fragments.Node;

namespace DataCollectionWizard.Client.Components.TreeNodeTemplates;

public sealed partial class DeviceTreeNodeTemplate : NodeTemplate
{
    private ActionButtonParameters? _actionButtonContainerParameters;
    private DropAreaParameters? _dropAreaParameters;

    // cast should always work because template will only be applied when cast is possible
    private NodeBase DeviceNode
        => (NodeBase)Node.TreeNode;

    protected override void Calculate()
    {
        // reset internal drop zone state on refresh if drop got disabled
        if (_dropAreaParameters is not null && !Node.DropZoneActive)
            _dropAreaParameters.CurrentlyOverDropZone = DropZone.None;

        _actionButtonContainerParameters?.CalculateCss();
    }

    protected override void CalculateCss(out IEnumerable<string> cssClasses, out IEnumerable<(string Property, string Value)> cssStyles)
    {
        var resCssClasses = new List<string>();

        if (!string.IsNullOrWhiteSpace(DeviceNode.Subtitle))
            resCssClasses.Add("has-subtitle");

        if (Node.Actions.OfType<IVisibleNodeAction>().Any())
            resCssClasses.Add("has-buttons");

        cssClasses = resCssClasses;
        cssStyles = [];
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (_dropAreaParameters is not null)
        {
            _dropAreaParameters.CalculateAllRequested -= CalculateAll;
            _dropAreaParameters.RefreshRequested -= Refresh;
        }

        Node.DragAndDropStateChanged -= OnDragAndDropStateChangedAsync;
    }

    private async void OnDragAndDropStateChangedAsync()
    {
        if (_dropAreaParameters is not null && !Node.DropZoneActive)
            _dropAreaParameters.CurrentlyOverDropZone = DropZone.None;

        await RefreshAsync();
    }

    protected override void OnInitialized()
    {
        // this is important because the base class overrides this itself too
        base.OnInitialized();

        _actionButtonContainerParameters = new() { Builder = Builder, Node = Node, };
        _actionButtonContainerParameters.CalculateCss();

        _dropAreaParameters = new() { Builder = Builder, Node = Node, };
        _dropAreaParameters.CalculateAllRequested += CalculateAll;
        _dropAreaParameters.RefreshRequested += Refresh;

        Node.DragAndDropStateChanged += OnDragAndDropStateChangedAsync;
    }

    protected override void OnPointerEnter(PointerEventArgs e)
    {
        if (!Builder.Settings.ActionsVisibility.HasFlag(ActionVisibility.Hover) || _actionButtonContainerParameters is null)
            return;

        _actionButtonContainerParameters.Hovering = true;

        CalculateAll();
        Refresh();
    }

    protected override void OnPointerLeave(PointerEventArgs e)
    {
        if (!Builder.Settings.ActionsVisibility.HasFlag(ActionVisibility.Hover) || _actionButtonContainerParameters is null)
            return;

        _actionButtonContainerParameters.Hovering = false;

        CalculateAll();
        Refresh();
    }
}
