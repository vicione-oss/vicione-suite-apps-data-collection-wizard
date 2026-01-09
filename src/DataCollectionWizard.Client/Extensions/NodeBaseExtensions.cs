using DataCollectionWizard.Client.Models;
using DataCollectionWizard.Client.Models.DeviceTree;

namespace DataCollectionWizard.Client.Extensions;

internal static class NodeBaseExtensions
{
    public static IEnumerable<NodeBase> GetNodeAndDescendants(this NodeBase node)
        => new[] { node }.Concat(node.Children.SelectMany(child => child.GetNodeAndDescendants()));

    public static bool HasError(this NodeBase node)
        => node.Status.HasFlag(NodeStatus.Offline);

    public static bool HasWarning(this NodeBase node)
        => node.Status.HasFlag(NodeStatus.NotSupported)
        || node.Status.HasFlag(NodeStatus.Unknown);

    public static bool IsNew(this NodeBase node)
        => node.Status.HasFlag(NodeStatus.New);
}
