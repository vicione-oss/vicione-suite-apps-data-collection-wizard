using DataCollectionWizard.Client.Components.TreeNodeTemplates;
using DataCollectionWizard.Client.Extensions;
using DataCollectionWizard.Client.Models;
using DataCollectionWizard.Client.Models.DeviceTree;
using DataCollectionWizard.Client.Resources;
using DataCollectionWizard.Internal.Services;
using ViciOne.Driver.IoTCore.Contracts.DeviceTree;
using ViciOne.Driver.IoTCore.Contracts.DeviceTree.Extensions;
using ViciOne.Ui.TreeEditor.Builder.Interface;
using ViciOne.Ui.TreeEditor.Builder.Interface.NodeActions;
using ViciOne.Ui.TreeEditor.Builder.Models;
using ViciOne.Ui.TreeEditor.Builder.Models.Icons;

namespace DataCollectionWizard.Client.Services;

internal sealed partial class DeviceTreeAdapter : TreeAdapter, IDisposable
{
    private readonly List<string> _expandedNodes = [];
    private readonly bool _isLiveView;
    private Root? _rootNode;
    private readonly Lock _setTreeLock = new();

    public IEnumerable<IDeviceTreeBase> DeviceTree
        => _rootNode is null ? [] : [_rootNode.Device];

    public event Action<NodeBase, NodeBase?>? NodeDeleted;
    public event Action<NodeBase>? NodeEdited;
    public event Action? SelectionChanged;

    public DeviceTreeAdapter(bool isLiveView)
    {
        _isLiveView = isLiveView;
        _selectionChangedTimer.Elapsed += OnSelectionChangedTimerElapsed;
    }

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

    public void CoupleTreeBuilderEvents()
    {
        // builder is available after the adapter was passed to TreeBuilder.SetAdapter method
        Builder.Selection.SelectionChanged += OnSelectionChanged;
        Builder.Expansion.ExpansionChanged += OnExpansionChanged;
    }

    public void Dispose()
    {
        Builder.Selection.SelectionChanged -= OnSelectionChanged;
        Builder.Expansion.ExpansionChanged -= OnExpansionChanged;

        _selectionChangedTimer.Elapsed -= OnSelectionChangedTimerElapsed;
        _selectionChangedTimer.Dispose();
    }

    public override IEnumerable<NodeActionButton> GetActions(ITreeNode node)
    {
        if (_isLiveView || node is not NodeBase baseNode)
            return [];

        var result = new List<NodeActionButton>();

        if (DeviceTreeNodeActionProvider.IsConfigurable(baseNode.Device))
        {
            result.Add(new()
            {
                Action = _ => Console.Out.WriteLine($"[configure] action invoked for [{baseNode.DisplayText}]"),
                Description = Localization.DeviceTreeAdapter.ConfigureNode,
                Icon = SvgIcons.cog_outline,
                Index = 0,
            });
        }

        if (DeviceTreeNodeActionProvider.IsEditable(baseNode.Device))
        {
            result.Add(new()
            {
                Action = args =>
                {
                    var nodeBase = (NodeBase)args.Node;
                    NodeEdited?.Invoke(nodeBase);
                },
                Description = Localization.DeviceTreeAdapter.EditAlias,
                Icon = RoccoSvgIcons.edit,
                Index = 0,
            });
        }

        if (DeviceTreeNodeActionProvider.IsDeletable(baseNode.Device))
        {
            result.Add(new()
            {
                Action = args =>
                {
                    var nodeBase = (NodeBase)args.Node;

                    foreach (var nodeOrChild in nodeBase.GetNodeAndDescendants())
                    {
                        _expandedNodes.Remove(GetPathToNode(nodeOrChild));
                    }

                    NodeDeleted?.Invoke(nodeBase, nodeBase.Parent);
                },
                Description = baseNode.Device is IDeviceTreeMasterNode ? Localization.DeviceTreeAdapter.DeleteDevice : Localization.DeviceTreeAdapter.DeleteNode,
                Icon = RoccoSvgIcons.delete,
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
        foreach (var selectedNode in _selected.ToArray())
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

    private bool IsRelevantChild(IDeviceTreeBase node)
         => _isLiveView
             ? node.Visible && node.GetNodeAndDescendants().OfType<IDeviceTreeLiveDataNode>().Any(n => n.Visible)
             : node.Visible;

    private void OnExpansionChanged(ITreeNode node)
    {
        if (node is not NodeBase baseNode)
            return;

        var pathToNode = GetPathToNode(baseNode);

        if (node.Expanded)
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

        // remove from selection
        var nodes = node.GetNodeAndDescendants().ToArray();
        _selected.RemoveAll(n => nodes.Contains(n));

        // remove node from TreeEditor tree
        var siblings = parentNode.Children.ToList();
        siblings.Remove(node);
        parentNode.Children = siblings;

        Builder.Notifications.NotifyChildrenChanged(parentNode);
    }

    internal void SetDeviceTree(DeviceTreeRoot root)
    {
        var nodesToExpandTo = new List<NodeBase>();
        var selectedNodeIds = Builder.Selection.SelectedNodes.OfType<NodeBase>().Select(n => n.Device.Id).ToArray();

        lock (_setTreeLock)
        {
            _selected.Clear();

            _rootNode = new Root
            {
                Device = root,
                DisplayText = DeviceTreeNodeNameProvider.GetTreeDisplayText(root),
                Expanded = true,
                IsLiveView = _isLiveView,
                Parent = null,
                Status = root.GetStatus(),
                Subtitle = DeviceTreeNodeSubTitleProvider.GetSubTitle(root),
            };

            var children = ResolveChildrenRecursive(root, _rootNode).ToArray();
            _rootNode.Children = children;
            _rootNode.HasChildren = children.Length > 0;
        }

        Builder.Notifications.NotifyRootNodesChanged();

        foreach (var node in nodesToExpandTo)
            Builder.Expansion.ExpandToNode(node);

        SelectionChanged?.Invoke();
        var nodeAndDescendants = _rootNode.GetNodeAndDescendants().ToArray();

        foreach (var selectedNodeId in selectedNodeIds)
        {
            SelectNode(selectedNodeId, nodeAndDescendants);
        }

        IEnumerable<NodeBase> ResolveChildrenRecursive(IDeviceTreeBase device, NodeBase parent, bool expandToOfflineNodes = false)
        {
            var result = new List<NodeBase>();
            foreach (var child in device.Children.Where(IsRelevantChild))
            {
                var node = new Node
                {
                    Device = child,
                    DisplayText = DeviceTreeNodeNameProvider.GetTreeDisplayText(child),
                    IsLiveView = _isLiveView,
                    Parent = parent,
                    Status = child.GetStatus(),
                    Subtitle = DeviceTreeNodeSubTitleProvider.GetSubTitle(child),
                };

                if (child is IDeviceTreeMasterNode)
                    expandToOfflineNodes = !node.Status.HasFlag(NodeStatus.Offline);

                if (expandToOfflineNodes && child is IDeviceTreeDataNode && node.Status.HasFlag(NodeStatus.Offline))
                    nodesToExpandTo.Add(node);

                node.Expanded = _expandedNodes.Contains(GetPathToNode(node));

                var children = ResolveChildrenRecursive(child, node, expandToOfflineNodes).ToArray();
                node.Children = children;
                node.HasChildren = children.Length > 0;

                result.Add(node);
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

    internal static Type? TemplateMapping(ITreeNode node, TemplateType templateType)
    {
        if (node is not NodeBase)
            return null;

        if (templateType == TemplateType.Node)
            return typeof(DeviceTreeNodeTemplate);

        return null;
    }
}
