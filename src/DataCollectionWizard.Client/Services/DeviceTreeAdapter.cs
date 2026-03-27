using DataCollectionWizard.Client.Components.TreeNodeTemplates;
using DataCollectionWizard.Client.Extensions;
using DataCollectionWizard.Client.Models;
using DataCollectionWizard.Client.Models.DeviceTree;
using DataCollectionWizard.Client.Resources;
using DataCollectionWizard.Internal.Services;
using ViciOne.Driver.IoTCore.Contracts.DeviceTree;
using ViciOne.Driver.IoTCore.Contracts.DeviceTree.Extensions;
using ViciOne.Ui.TreeEditor.Builder.Interface;
using ViciOne.Ui.TreeEditor.Builder.Interface.Enums;
using ViciOne.Ui.TreeEditor.Builder.Interface.Icons;
using ViciOne.Ui.TreeEditor.Builder.Interface.NodeActions;
using ViciOne.Ui.TreeEditor.Builder.Interface.Nodes;

namespace DataCollectionWizard.Client.Services;

internal sealed class DeviceTreeAdapter(bool isLiveView) : TreeAdapter
{
    private readonly List<string> _expandedNodes = [];
    private Root? _rootNode;
    private readonly Lock _setTreeLock = new();

    public IEnumerable<IDeviceTreeBase> DeviceTree
        => _rootNode is null ? [] : [_rootNode.Device];

    public event Action<NodeBase, NodeBase?>? NodeDeleted;
    public event Action<NodeBase>? NodeEdited;
    public event Action? SelectionChanged;

    public override bool CanSelectNode(ITreeNode node, IEnumerable<ITreeNode> currentSelection, bool willDeselectOthers)
        => node is not Root;

    internal static int CountEnabledDataPoints(IDeviceTreeBase[] treeNodes)
    {
        var enabledCompressableDataNodes = treeNodes.OfType<IDeviceTreeCompressableDataNode>()
            .SelectMany(cdn => cdn.CompressorConfigurations)
            .Count(cc => cc.Enabled);

        var enabledSchedulableDataNodes = treeNodes.OfType<IDeviceTreeSchedulableDataNode>()
            .SelectMany(sdn => sdn.SchedulerConfigurations)
            .Count(sc => sc.Enabled);

        var enabledEventTriggers = treeNodes.OfType<IDeviceTreeEventTriggerDataNode>()
            .SelectMany(etdn => etdn.EventTriggerConfigurations.SelectMany(tc => tc.Triggers))
            .Count(t => t.Enabled);

        return enabledCompressableDataNodes + enabledSchedulableDataNodes + enabledEventTriggers;
    }

    public override void Dismantle()
    {
        Builder.Expansion.ExpansionChanged -= OnExpansionChanged;
        Builder.Selection.SelectionChanged -= OnSelectionChanged;
    }

    private void OnSelectionChanged(ITreeNode node, bool selected)
    {
        if (node is not NodeBase)
            return;

        SelectionChanged?.Invoke();
    }

    public override IEnumerable<INodeAction> GetActions(ITreeNode node)
    {
        if (isLiveView || node is not NodeBase baseNode)
            return [];

        var result = new List<INodeAction>();

        if (DeviceTreeNodeActionProvider.IsConfigurable(baseNode.Device))
        {
            result.Add(new NodeButton()
            {
                Action = (_, _) => Console.Out.WriteLine($"[configure] action invoked for [{baseNode.DisplayText}]"),
                Description = Localization.DeviceTreeAdapter.ConfigureNode,
                Icon = new SvgIcon(SvgIcons.cog_outline),
                Index = 0,
            });
        }

        if (DeviceTreeNodeActionProvider.IsEditable(baseNode.Device))
        {
            result.Add(new NodeButton()
            {
                Action = (s, args) =>
                {
                    var nodeBase = (NodeBase)args.Node;
                    NodeEdited?.Invoke(nodeBase);
                },
                Description = Localization.DeviceTreeAdapter.EditAlias,
                Icon = new SvgIcon(RoccoSvgIcons.edit),
                Index = 0,
            });
        }

        if (DeviceTreeNodeActionProvider.IsDeletable(baseNode.Device))
        {
            result.Add(new NodeButton()
            {
                Action = (s, args) =>
                {
                    var nodeBase = (NodeBase)args.Node;

                    foreach (var nodeOrChild in nodeBase.GetNodeAndDescendants())
                    {
                        _expandedNodes.Remove(GetPathToNode(nodeOrChild));
                    }

                    NodeDeleted?.Invoke(nodeBase, nodeBase.Parent);
                },
                Description = baseNode.Device is IDeviceTreeMasterNode ? Localization.DeviceTreeAdapter.DeleteDevice : Localization.DeviceTreeAdapter.DeleteNode,
                Icon = new SvgIcon(RoccoSvgIcons.delete),
                Index = 1,
            });
        }

        return result;
    }

    public override IEnumerable<ITreeNode> GetChildren(ITreeNode node)
    {
        if (node is not NodeBase baseNode)
            return [];

        return baseNode.Children;
    }

    public override string GetDisplayText(ITreeNode node)
    {
        if (node is not NodeBase baseNode)
            return string.Empty;

        return baseNode.DisplayText;
    }

    public override IEnumerable<IIcon> GetIcons(ITreeNode node)
    {
        if (node is not NodeBase baseNode)
            return [];

        return [DeviceTreeNodeIconProvider.GetIcon(baseNode.Device)];
    }

    private static string GetPathToNode(NodeBase node)
    {
        var path = GetPathToNodeRecursive(node);
        path.Reverse();
        return string.Join("-", path);

        static List<string> GetPathToNodeRecursive(NodeBase node)
        {
            List<string> path = [];
            path.Add(node.DisplayText);

            if (node.Parent is null)
                return path;

            path.AddRange(GetPathToNodeRecursive(node.Parent));
            return path;
        }
    }

    public override ITreeNode? GetParent(ITreeNode node)
    {
        if (node is not NodeBase baseNode)
            return null;

        return baseNode.Parent;
    }

    public IEnumerable<IDeviceTreeDataNode> GetRelevantDataNodes()
    {
        var selectedNodesAndDescendants = new List<NodeBase>();
        foreach (var selectedNode in Builder.Selection.SelectedNodes.OfType<NodeBase>().ToArray())
            selectedNodesAndDescendants.AddRange(selectedNode.GetNodeAndDescendants());

        var filteredNodes = Builder.Filter.Apply(selectedNodesAndDescendants.Distinct()).OfType<NodeBase>();
        return [.. filteredNodes.Select(fn => fn.Device).OfType<IDeviceTreeDataNode>()];
    }

    public override IEnumerable<ITreeNode> GetRootNodes()
        => _rootNode is null ? [] : [_rootNode];

    public override bool HasChildren(ITreeNode node)
    {
        if (node is not NodeBase baseNode)
            return false;

        return baseNode.HasChildren;
    }

    public override bool HasFilterMatchingDescendants(ITreeNode node, Predicate<ITreeNode> filter)
    {
        if (node is not NodeBase baseNode)
            return false;

        foreach (var descendant in GetNodeAndDescendants(baseNode).Except([baseNode]).ToArray())
        {
            if (filter.Invoke(descendant))
                return true;
        }

        return false;

        static IEnumerable<NodeBase> GetNodeAndDescendants(NodeBase node)
            => new[] { node }.Concat(node.Children.SelectMany(child => GetNodeAndDescendants(child)));
    }

    public override bool IsExpanded(ITreeNode node)
        => (node as NodeBase)?.Expanded ?? false;


    private bool IsRelevantChild(IDeviceTreeBase node)
         => isLiveView
             ? node.Visible && node.GetNodeAndDescendants().OfType<IDeviceTreeLiveDataNode>().Any(n => n.Visible)
             : node.Visible;

    private void OnExpansionChanged(ITreeNode node, bool expanded)
    {
        if (node is not NodeBase baseNode)
            return;

        baseNode.Expanded = expanded;
        var pathToNode = GetPathToNode(baseNode);

        if (baseNode.Expanded)
            _expandedNodes.Add(pathToNode);
        else
            _expandedNodes.Remove(pathToNode);
    }

    public void RemoveNodeFromParent(NodeBase node, NodeBase? parentNode)
    {
        if (parentNode is null)
            return;

        // remove node from device tree
        parentNode.Device.Children.Remove(node.Device);

        // remove node from TreeEditor tree
        var siblings = parentNode.Children.ToList();
        siblings.Remove(node);
        parentNode.Children = siblings;

        Builder.Notifications.NotifyChildrenChanged(parentNode);
    }

    internal void SetDeviceTree(DeviceTreeRoot root, bool expandOfflineNodes)
    {
        var nodesToExpandTo = new List<NodeBase>();
        var selectedNodeIds = Builder.Selection.SelectedNodes.OfType<NodeBase>().Select(n => n.Device.Id).ToArray();
        var rootNodeReused = _rootNode is not null;

        lock (_setTreeLock)
        {
            if (_rootNode is null)
            {
                _rootNode = new Root()
                {
                    Device = root,
                    Expanded = true,
                    Id = new StringTreeNodeIdentifier() { Value = root.Id, },
                    IsLiveView = isLiveView,
                    Parent = null,
                    DisplayText = DeviceTreeNodeNameProvider.GetTreeDisplayText(root),
                };
            }
            else
            {
                _rootNode.Device = root;
                _rootNode.DisplayText = DeviceTreeNodeNameProvider.GetTreeDisplayText(_rootNode.Device);
            }

            _rootNode.Status = _rootNode.Device.GetStatus();
            _rootNode.Subtitle = DeviceTreeNodeSubTitleProvider.GetSubTitle(_rootNode.Device);

            var children = ResolveChildrenRecursive(_rootNode.Device, _rootNode, expandOfflineNodes).ToArray();
            _rootNode.Children = children;
            _rootNode.HasChildren = children.Length > 0;
        }

        if (rootNodeReused)
        {
            Builder.Notifications.NotifyNodeChanged(_rootNode);
            Builder.Notifications.NotifyChildrenChanged(_rootNode);
            Builder.Helper.RequestNodeRefresh(_rootNode);
        }
        else
        {
            Builder.Notifications.NotifyRootNodesChanged();
        }

        foreach (var node in nodesToExpandTo)
            Builder.Expansion.ExpandToNode(node);

        SelectionChanged?.Invoke();
        var nodeAndDescendants = _rootNode.GetNodeAndDescendants().ToArray();

        foreach (var selectedNodeId in selectedNodeIds)
        {
            SelectNode(selectedNodeId, nodeAndDescendants);
        }

        IEnumerable<NodeBase> ResolveChildrenRecursive(IDeviceTreeBase device, NodeBase parent, bool expandAnyOfflineNodes, bool expandToOfflineNodes = false)
        {
            var result = new List<NodeBase>();
            foreach (var childDevice in device.Children.Where(IsRelevantChild))
            {
                // check and possibly re-use existing child node
                var childNode = parent.Children.FirstOrDefault(c => ((StringTreeNodeIdentifier)c.Id).Equals(childDevice.Id));
                var childNodeReused = childNode is not null;

                if (childNode is null)
                {
                    childNode = new Node
                    {
                        Device = childDevice,
                        DisplayText = DeviceTreeNodeNameProvider.GetTreeDisplayText(childDevice),
                        Id = new StringTreeNodeIdentifier() { Value = childDevice.Id, },
                        IsLiveView = isLiveView,
                        Parent = parent,
                    };
                }
                else
                {
                    childNode.Device = childDevice;
                    childNode.DisplayText = DeviceTreeNodeNameProvider.GetTreeDisplayText(childNode.Device);
                }

                childNode.Status = childNode.Device.GetStatus();
                childNode.Subtitle = DeviceTreeNodeSubTitleProvider.GetSubTitle(childNode.Device);

                if (childNode.Device is IDeviceTreeMasterNode)
                    expandToOfflineNodes = !childNode.Status.HasFlag(NodeStatus.Offline) && expandAnyOfflineNodes;

                if (expandToOfflineNodes && childNode.Device is IDeviceTreeDataNode && childNode.Status.HasFlag(NodeStatus.Offline))
                    nodesToExpandTo.Add(childNode);

                childNode.Expanded = _expandedNodes.Contains(GetPathToNode(childNode));

                var children = ResolveChildrenRecursive(childNode.Device, childNode, expandAnyOfflineNodes, expandToOfflineNodes).ToArray();
                childNode.Children = children;
                childNode.HasChildren = children.Length > 0;

                result.Add(childNode);

                if (childNodeReused)
                {
                    Builder.Notifications.NotifyNodeChanged(childNode);
                    Builder.Notifications.NotifyChildrenChanged(childNode);
                    Builder.Helper.RequestNodeRefresh(childNode);
                }
            }
            return result;
        }
    }

    private void SelectNode(string selectedNodeId, IEnumerable<NodeBase> allNodes)
    {
        var node = allNodes.FirstOrDefault(n => n.Device.Id == selectedNodeId);

        if (node is not null)
        {
            Builder.Selection.ChangeSelection(node, true);
        }
    }

    internal static void SortEventTriggers(IDeviceTreeBase[] treeNodes)
    {
        var nodeNames = treeNodes.ToDictionary(n => n.Id, n => n.Name);

        foreach (var triggerNode in treeNodes.OfType<IDeviceTreeEventTriggerDataNode>())
        {
            DeviceTreeBuilder.SortSensors(triggerNode, nodeNames);
        }
    }

    internal static void SortNodeChildren(IEnumerable<IDeviceTreeBase> treeNodes)
    {
        foreach (var child in treeNodes.Where(node => node.Children.Count > 0))
            child.Children.Sort(DeviceTreeBaseComparer.Default);
    }

    public override void Setup()
    {
        Builder.Selection.SelectionChanged += OnSelectionChanged;
        Builder.Expansion.ExpansionChanged += OnExpansionChanged;

        // apply default settings
        Builder.Settings.ActionsAlignment = ActionAlignment.ByMax;
        Builder.Settings.ActionsVisibility = ActionVisibility.Hover;

        Builder.DragAndDrop.EnableInbound = false;
        Builder.DragAndDrop.EnableInternal = false;
        Builder.DragAndDrop.EnableOutbound = false;

        Builder.Guidelines.Show = true;
        Builder.Template.Mapping = TemplateMapping;
    }

    private static Type? TemplateMapping(ITreeNode node, TemplateType templateType)
    {
        if (node is not NodeBase)
            return null;

        if (templateType == TemplateType.Node)
            return typeof(DeviceTreeNodeTemplate);

        return null;
    }
}
