using ViciOne.Driver.IoTCore.Contracts.DeviceTree;

namespace DataCollectionWizard.Client.Components.ManagementGrid.Models;

public sealed record ManagementGridRowModel
{
    public required IDeviceTreeDataNode DataNode { get; set; }
    public bool IsExpanded { get; set; }
    public required IEnumerable<IDeviceTreeBase> PathToNode { get; set; }
}
