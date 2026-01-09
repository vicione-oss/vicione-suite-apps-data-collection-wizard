using ViciOne.Driver.IoTCore.Contracts.DeviceTree;

namespace DataCollectionWizard.Client.Extensions;

internal static class DeviceTreeBaseExtensions
{
    public static int CaptionCount(this IDeviceTreeBase device)
    {
        var count = 0;

        if (device is IDeviceTreeConfigurableRawDataNode)
            count++;

        if (device is IDeviceTreeSchedulableDataNode)
            count++;

        if (device is IDeviceTreeEventTriggerDataNode)
            count++;

        return count;
    }

    public static bool HasCaptions(this IDeviceTreeBase device)
        => device.CaptionCount() > 0;
}
