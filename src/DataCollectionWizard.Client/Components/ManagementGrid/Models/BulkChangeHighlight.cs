using ViciOne.DeviceTree.Contracts;

namespace DataCollectionWizard.Client.Components.ManagementGrid.Models;

/// <summary>
/// The cells a bulk change actually reached, so the grid can point them out afterwards.
/// </summary>
/// <remarks>
/// A bulk change rarely covers the whole selection: a setting only applies to the data types that support it,
/// and only to the publish targets it was aimed at. The page collects what it wrote while it writes it, rather
/// than the grid guessing from the selection - a cell that lights up without having changed is worse than no
/// highlight at all.
/// </remarks>
/// <param name="Nodes">The data nodes whose configuration was written.</param>
/// <param name="TargetIds">The publish targets that were written.</param>
internal sealed record BulkChangeHighlight(
    IReadOnlySet<IDeviceTreeDataNode> Nodes,
    IReadOnlySet<Guid> TargetIds)
{
    /// <summary>
    /// Nothing to point out - used when a change was rejected before it reached any configuration.
    /// </summary>
    public static BulkChangeHighlight None { get; } = new(
        new HashSet<IDeviceTreeDataNode>(),
        new HashSet<Guid>());

    /// <summary>
    /// Whether the cell for <paramref name="node"/> under <paramref name="targetId"/> was written.
    /// </summary>
    public bool Covers(IDeviceTreeDataNode node, Guid targetId)
        => TargetIds.Contains(targetId) && Nodes.Contains(node);
}
