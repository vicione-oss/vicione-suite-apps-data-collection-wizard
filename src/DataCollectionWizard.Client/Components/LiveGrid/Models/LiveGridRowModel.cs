using DataCollectionWizard.Client.Extensions;
using ViciOne.DeviceTree.Contracts;

namespace DataCollectionWizard.Client.Components.LiveGrid.Models;

public sealed record LiveGridRowModel
{
    private string? _breadcrumb;

    public required IDeviceTreeDataNode DataNode { get; set; }
    public string LastUpdated { get; set; } = string.Empty;
    public required IEnumerable<IDeviceTreeBase> PathToNode { get; set; }
    public string Unit { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// Lazily computed and cached breadcrumb path string.
    /// </summary>
    public string Breadcrumb => _breadcrumb ??= PathToNode.GetBreadcrumb();
}
