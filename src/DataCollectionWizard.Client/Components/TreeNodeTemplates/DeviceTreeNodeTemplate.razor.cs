using DataCollectionWizard.Client.Models.DeviceTree;
using Microsoft.AspNetCore.Components.Web;
using ViciOne.Ui.TreeEditor.Builder.Interface;
using ViciOne.Ui.TreeEditor.Builder.Interface.NodeActions;
using ViciOne.Ui.TreeEditor.Builder.Models;
using ViciOne.Ui.TreeEditor.Templates;

namespace DataCollectionWizard.Client.Components.TreeNodeTemplates;

public sealed partial class DeviceTreeNodeTemplate : NodeTemplateBase, IDisposable
{
    private string _actionButtonContainerCssClasses = string.Empty;
    private MoveTarget _currentlyOverDropZone;
    private bool _shouldRender;
    private string _treeNodeCssClasses = string.Empty;
    private string _treeNodeCssStyles = string.Empty;

    private NodeBase DeviceNode
        => (NodeBase)Node.TreeNode; // cast should always work because template will only be applied when cast is possible

    private void CalculateActionButtonContainerCssClasses()
    {
        var cssClasses = new List<string>();

        if (Builder.Settings.ActionsVisibility.HasFlag(ActionVisibility.Always))
        {
            cssClasses.Add("visible");
        }
        else
        {
            if (Builder.Settings.ActionsVisibility.HasFlag(ActionVisibility.Hover))
                cssClasses.Add("visible-on-hover");

            if (Node.TreeNode.Selected && Builder.Selection.SelectedNodes.Count() == 1)
                cssClasses.Add("visible");
        }

        _actionButtonContainerCssClasses = cssClasses.Count > 0
            ? string.Join(' ', cssClasses)
            : string.Empty;
    }

    private async void CalculateAllAsync()
    {
        CalculateTreeNodeCssClasses();
        CalculateActionButtonContainerCssClasses();
        await RefreshAsync();
    }

    private void CalculateTreeNodeCssClasses()
    {
        var cssClasses = new List<string>();

        if (Builder.Settings.ActionsVisibility.HasFlag(ActionVisibility.Hover))
            cssClasses.Add("actions-visible-on-hover");

        if (Builder.DragAndDrop.DraggingNode is not null && Node.TreeNode.Selected)
            cssClasses.Add("dragging");

        if (Builder.DragAndDrop.DraggingNode == Node.TreeNode)
            cssClasses.Add("origin");

        if (_currentlyOverDropZone != MoveTarget.None)
        {
            cssClasses.Add("drop-zone");
#pragma warning disable CA1308 // Normalize strings to uppercase - useless because it is needed for css
            cssClasses.Add(_currentlyOverDropZone.ToString().ToLowerInvariant());
#pragma warning restore CA1308 // Normalize strings to uppercase
            cssClasses.Add(Node.ValidDropZones.HasFlag(_currentlyOverDropZone) ? "valid" : "invalid");
        }

        if (Node.TreeNode.Selected)
            cssClasses.Add("selected");

        if (!string.IsNullOrWhiteSpace(DeviceNode.Subtitle))
            cssClasses.Add("has-subtitle");

        cssClasses.AddRange(Builder.Adapter.GetCssClasses(Node.TreeNode, TemplateType.Node));

        _treeNodeCssStyles = string.Join(' ', Builder.Adapter.GetCssStyles(Node.TreeNode, TemplateType.Node).Select(st => $"{st.Property}:{st.Value};"));
        _treeNodeCssClasses = cssClasses.Count > 0
            ? string.Join(' ', cssClasses)
            : string.Empty;
    }

    public void Dispose()
    {
        Node.DragAndDropStateChanged -= DragAndDropStateChangedAsync;
        Node.Refresh -= CalculateAllAsync;
    }

    private async void DragAndDropStateChangedAsync()
    {
        if (!Node.DropZoneActive)
            _currentlyOverDropZone = MoveTarget.None;

        await RefreshAsync();
    }

    protected override void OnInitialized()
    {
        Node.DragAndDropStateChanged += DragAndDropStateChangedAsync;
        Node.Refresh += CalculateAllAsync;

        CalculateAllAsync();
    }

    private void OnNodeActionButtonClicked(INodeAction action, MouseEventArgs e)
    {
        if (!action.EnabledFunc(Node.TreeNode))
            return;

        action.Action.Invoke(new(Node.TreeNode, action, Builder, e));
    }

    private void OnNodeDragEnd(DragEventArgs e)
        => Builder.DragAndDrop.EndNodeDrag(true);

    private void OnNodeDragStart(DragEventArgs e)
        => Builder.DragAndDrop.StartNodeDrag(Node.TreeNode);

    private async Task RefreshAsync()
    {
        _shouldRender = true;
        await InvokeAsync(StateHasChanged);
    }

    private bool RenderActionButtons()
        => (Builder.Settings.ActionsVisibility.HasFlag(ActionVisibility.Always)
            || Builder.Settings.ActionsVisibility.HasFlag(ActionVisibility.Hover)
            || (Node.TreeNode.Selected && Builder.Selection.SelectedNodes.Count() == 1))
            && Node.Actions.Any();

    protected override bool ShouldRender()
    {
        if (_shouldRender)
        {
            _shouldRender = false;
            return true;
        }

        return false;
    }
}
