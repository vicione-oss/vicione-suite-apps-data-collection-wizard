using ViciOne.Driver.IoTCore.Contracts.DeviceTree;
using ViciOne.Ui.TreeEditor.Builder.Interface;

namespace DataCollectionWizard.Client.Models.DeviceTree;

internal abstract class NodeBase : ITreeNode
{
    public IEnumerable<NodeBase> Children { get; set; } = [];
    public required IDeviceTreeBase Device { get; set; }
    public required string DisplayText { get; set; }
    public bool Expanded { get; set; }
    public bool HasChildren { get; set; }
    public Guid Id { get; set; } = Guid.NewGuid();
    public bool IsLiveView { get; set; }
    public NodeBase? Parent { get; set; }
    public bool Selected { get; set; }
    public string? Subtitle { get; set; }
    public NodeStatus Status { get; set; }
}
