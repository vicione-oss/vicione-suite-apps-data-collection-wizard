using DataCollectionWizard.Client.Extensions;
using ViciOne.Driver.IoTCore.Contracts.DeviceTree;

namespace DataCollectionWizard.Client.Components.ManagementGrid.Models;

public sealed record ManagementGridRowModel
{
    public required IDeviceTreeDataNode DataNode { get; set; }
    public bool IsExpanded { get; set; }
    public required IEnumerable<IDeviceTreeBase> PathToNode { get; set; }

    private string? _breadcrumb;

    /// <summary>Lazily computed and cached breadcrumb path string.</summary>
    public string Breadcrumb => _breadcrumb ??= PathToNode.GetBreadcrumb();
}
