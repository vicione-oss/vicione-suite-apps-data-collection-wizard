using ViciOne.DeviceTree.Contracts;

namespace DataCollectionWizard.Client.Components.LiveGrid.Models;

public sealed record LiveGridRowModel
{
    public required IDeviceTreeDataNode DataNode { get; set; }
    public string LastUpdated { get; set; } = string.Empty;
    public required IEnumerable<IDeviceTreeBase> PathToNode { get; set; }
    public string Unit { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}
