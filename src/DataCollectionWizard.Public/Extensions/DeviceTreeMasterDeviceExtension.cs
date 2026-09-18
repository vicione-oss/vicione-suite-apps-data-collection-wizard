using ViciOne.DeviceTree.Contracts;

namespace DataCollectionWizard.Public.Extensions;

public static class DeviceTreeMasterDeviceExtension
{
    public const string MasterDeviceName = "IO-Link Master";
    public const string VseDeviceName = "VSE Device";

    public static string GetMacAddress(this IDeviceTreeMasterNode deviceTreeMaster)
    {
        var macAddress = deviceTreeMaster switch
        {
            DeviceTreeVseDevice vse => vse.MacAddress ?? string.Empty,
            DeviceTreeIoLinkMaster ioLink => ioLink.MacAddress,
            _ => throw new NotImplementedException($"Getting the MAC address for {deviceTreeMaster.GetType().Name} is not implemented."),
        };

        if (string.IsNullOrEmpty(macAddress))
            macAddress = "00:00:00:00:00:00";

        return macAddress;
    }
}
