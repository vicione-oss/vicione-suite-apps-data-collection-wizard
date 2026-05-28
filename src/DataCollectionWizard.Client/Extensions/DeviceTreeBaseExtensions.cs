using ViciOne.Driver.IoTCore.Contracts.DeviceTree;

namespace DataCollectionWizard.Client.Extensions;

internal static class DeviceTreeBaseExtensions
{
    public static int CaptionCount(this IDeviceTreeBase device)
    {
        var count = 0;

        if (device is IDeviceTreeConfigurableRawDataNode)
            count++;

        // DeviceTreeBlobData is an IDeviceTreeSchedulableDataNode but its scheduler
        // caption is intentionally not rendered (see DeviceIdentifierCell.razor),
        // so it must not be counted here either - otherwise the cell wrapper
        // reserves an empty caption row plus grid-row-gap, making the row taller
        // than the other grid rows.
        if (device is IDeviceTreeSchedulableDataNode and not DeviceTreeBlobData)
            count++;

        if (device is IDeviceTreeEventTriggerDataNode)
            count++;

        return count;
    }

    public static bool HasCaptions(this IDeviceTreeBase device)
        => device.CaptionCount() > 0;
}
