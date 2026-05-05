using DataCollectionWizard.Client.Models;
using DataCollectionWizard.Client.Models.DeviceTree;

namespace DataCollectionWizard.Client.Extensions;

internal static class NodeBaseExtensions
{
    public static IEnumerable<NodeBase> GetNodeAndDescendants(this NodeBase node)
        => new[] { node }.Concat(node.Children.SelectMany(child => child.GetNodeAndDescendants()));

    public static bool GetFilterResult(this NodeBase node, string filterText)
        => node.DisplayText.Contains(filterText, StringComparison.InvariantCultureIgnoreCase)
        || (!string.IsNullOrWhiteSpace(node.Subtitle) && node.Subtitle.Contains(filterText, StringComparison.InvariantCultureIgnoreCase));

    public static bool HasError(this NodeBase node)
        => node.Status.HasFlag(NodeStatus.Offline);

    public static bool HasInheritedError(this NodeBase node)
        => node.InheritedStatus.HasFlag(NodeStatus.Offline);

    public static bool HasWarning(this NodeBase node)
        => node.Status.HasFlag(NodeStatus.NotSupported)
        || node.Status.HasFlag(NodeStatus.Unknown);

    public static bool HasInheritedWarning(this NodeBase node)
        => node.InheritedStatus.HasFlag(NodeStatus.NotSupported)
        || node.InheritedStatus.HasFlag(NodeStatus.Unknown);

    public static bool IsNew(this NodeBase node)
        => node.Status.HasFlag(NodeStatus.New);

    public static bool IsInheritedNew(this NodeBase node)
        => node.InheritedStatus.HasFlag(NodeStatus.New);
}
