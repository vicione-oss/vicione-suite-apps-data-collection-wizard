using ViciOne.DeviceTree.Contracts;
using ViciOne.Ui.TreeEditor.Builder.Interface.NodeIdentifier;
using ViciOne.Ui.TreeEditor.Builder.Interface.Nodes;

namespace DataCollectionWizard.Client.Models.DeviceTree;

internal abstract class NodeBase : ITreeNode
{
    public IEnumerable<NodeBase> Children { get; set; } = [];
    public required IDeviceTreeBase Device { get; set; }
    public required string DisplayText { get; set; }
    public bool Expanded { get; set; }
    public bool HasChildren { get; set; }
    public required INodeIdentifier Id { get; set; }
    public bool IsLiveView { get; set; }
    public NodeBase? Parent { get; set; }
    public string? Subtitle { get; set; }
    public NodeStatus Status { get; set; }
    public NodeStatus InheritedStatus { get; set; }
}
