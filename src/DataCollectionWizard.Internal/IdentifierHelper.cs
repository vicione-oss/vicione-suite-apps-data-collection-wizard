using ViciOne.Driver.IoTCore.Contracts.Constants;
using ViciOne.Driver.IoTCore.Contracts.DeviceTree;

namespace DataCollectionWizard.Internal;

public static class IdentifierHelper
{
    private static string GetAliasOrName(IAliasStructureNode node)
        => string.IsNullOrWhiteSpace(node.Alias) ? node.Name : node.Alias;

    private static string GetIoLinkDeviceAddressWithPort(Uri uri)
        => $"{uri.DnsSafeHost}:{uri.Port}";

    public static string GetIoLinkIdentifier(DeviceTreeIoLinkMaster masterDevice, DeviceTreeIoLinkMasterPort ioLinkMasterPort, DeviceTreeDevice device, DeviceTreeProcessData processData, PoolingMode poolingMode, int poolingGrid)
        => Identifiers.GetIoLinkMasterIdentifier(GetIoLinkDeviceAddressWithPort(masterDevice.Url),
            device.ApplicationSpecificTag ?? string.Empty, ioLinkMasterPort.SubIndex, device.VendorId, device.DeviceId, device.Name,
            processData.Name, GetPoolingModeString(poolingMode, poolingGrid));

    private static string GetPoolingModeString(PoolingMode poolingMode, int poolingGrid)
        => poolingGrid == -1 ? "OnChange" : poolingMode.ToString();

    public static string GetVseAlarmIdentifier(DeviceTreeVseDevice vseDevice, DeviceTreeVseAlarm vseAlarm, DeviceTreeProcessData processData,
        PoolingMode poolingMode, int poolingGrid)
        => Identifiers.GetVseAlarmIdentifier(GetVseAddressWithPort(vseDevice.Url), GetAliasOrName(vseAlarm), vseAlarm.Type, processData.Name, GetPoolingModeString(poolingMode, poolingGrid));

    private static string GetVseAddressWithPort(Uri url)
        => $"{url.DnsSafeHost}:{url.Port}";

    public static string GetVseCounterIdentifier(DeviceTreeVseDevice vseDevice, DeviceTreeVseCounter vseCounter, IDeviceTreeCompressableDataNode processData,
        PoolingMode poolingMode, int poolingGrid)
        => Identifiers.GetVseCounterIdentifier(GetVseAddressWithPort(vseDevice.Url), GetAliasOrName(vseCounter), vseCounter.Type, vseCounter.Unit, processData.Name,
            GetPoolingModeString(poolingMode, poolingGrid));

    public static string GetVseInputIdentifier(DeviceTreeVseDevice vseDevice, DeviceTreeVseInput vseInput, IDeviceTreeCompressableDataNode processData,
        PoolingMode poolingMode, int poolingGrid, string inputType)
        => Identifiers.GetVseInputIdentifier(GetVseAddressWithPort(vseDevice.Url), inputType, vseInput.InputId, GetAliasOrName(vseInput),
            vseInput.Unit, processData.Name, GetPoolingModeString(poolingMode, poolingGrid));

    public static string GetVseObjectIdentifier(DeviceTreeVseDevice vseDevice, DeviceTreeVseObject vseObject, IDeviceTreeCompressableDataNode processData,
        PoolingMode poolingMode, int poolingGrid)
    {
        var unit = processData.Name is "Average" or "Maximum" or "Minimum" or "Damage" or "Warning"
            ? vseObject.Unit
            : string.Empty;

        return Identifiers.GetVseObjectIdentifier(GetVseAddressWithPort(vseDevice.Url), vseObject.InputType, vseObject.InputId, GetAliasOrName(vseObject), vseObject.ObjectId,
            vseObject.Type, unit, processData.Name, GetPoolingModeString(poolingMode, poolingGrid));
    }

    public static string GetVseVariantIdentifier(DeviceTreeVseDevice vseDevice, PoolingMode poolingMode, int poolingGrid)
        => Identifiers.GetVseVariantIdentifier(GetVseAddressWithPort(vseDevice.Url), GetPoolingModeString(poolingMode, poolingGrid));
}
