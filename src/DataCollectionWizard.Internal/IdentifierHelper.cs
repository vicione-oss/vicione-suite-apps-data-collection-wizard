using ViciOne.DeviceTree.Contracts;

namespace DataCollectionWizard.Internal;

internal static class IdentifierHelper
{
    private static string GetAliasOrName(IDeviceTreeDeviceAliasNode node)
        => string.IsNullOrWhiteSpace(node.Alias) ? node.Name : node.Alias;

    private static string GetIoLinkDeviceAddressWithPort(Uri uri)
        => $"{uri.DnsSafeHost}:{uri.Port}";

    public static string GetIoLinkIdentifier(DeviceTreeIoLinkMaster masterDevice, DeviceTreeIoLinkMasterPort ioLinkMasterPort, DeviceTreeDevice device, DeviceTreeProcessData processData, AggregationFunction aggregationFunction, int poolingGrid)
        => Identifiers.GetIoLinkMasterIdentifier(GetIoLinkDeviceAddressWithPort(masterDevice.Url),
            device.ApplicationSpecificTag ?? string.Empty, ioLinkMasterPort.SubIndex, device.VendorId, device.DeviceId, device.Name,
            processData.Name, GetAggregationFunctionString(aggregationFunction, poolingGrid));

    private static string GetAggregationFunctionString(AggregationFunction aggregationFunction, int poolingGrid)
        => poolingGrid == -1 ? "OnChange" : aggregationFunction.ToString();

    public static string GetVseAlarmIdentifier(DeviceTreeVseDevice vseDevice, DeviceTreeVseAlarm vseAlarm, DeviceTreeProcessData processData,
        AggregationFunction aggregationFunction, int poolingGrid)
        => Identifiers.GetVseAlarmIdentifier(GetVseAddressWithPort(vseDevice.Url), GetAliasOrName(vseAlarm), vseAlarm.Type, processData.Name, GetAggregationFunctionString(aggregationFunction, poolingGrid));

    private static string GetVseAddressWithPort(Uri url)
        => $"{url.DnsSafeHost}:{url.Port}";

    public static string GetVseCounterIdentifier(DeviceTreeVseDevice vseDevice, DeviceTreeVseCounter vseCounter, IDeviceTreeCompressableDataNode processData,
        AggregationFunction aggregationFunction, int poolingGrid)
        => Identifiers.GetVseCounterIdentifier(GetVseAddressWithPort(vseDevice.Url), GetAliasOrName(vseCounter), vseCounter.Type, vseCounter.Unit, processData.Name,
            GetAggregationFunctionString(aggregationFunction, poolingGrid));

    public static string GetVseInputIdentifier(DeviceTreeVseDevice vseDevice, DeviceTreeVseInput vseInput, IDeviceTreeCompressableDataNode processData,
        AggregationFunction aggregationFunction, int poolingGrid, string inputType)
        => Identifiers.GetVseInputIdentifier(GetVseAddressWithPort(vseDevice.Url), inputType, vseInput.InputId, GetAliasOrName(vseInput),
            vseInput.Unit, processData.Name, GetAggregationFunctionString(aggregationFunction, poolingGrid));

    public static string GetVseObjectIdentifier(DeviceTreeVseDevice vseDevice, DeviceTreeVseObject vseObject, IDeviceTreeCompressableDataNode processData,
        AggregationFunction aggregationFunction, int poolingGrid)
    {
        var unit = processData.Name is "Average" or "Maximum" or "Minimum" or "Damage" or "Warning"
            ? vseObject.Unit
            : string.Empty;

        return Identifiers.GetVseObjectIdentifier(GetVseAddressWithPort(vseDevice.Url), vseObject.InputType, vseObject.InputId, GetAliasOrName(vseObject), vseObject.ObjectId,
            vseObject.Type, unit, processData.Name, GetAggregationFunctionString(aggregationFunction, poolingGrid));
    }

    public static string GetVseVariantIdentifier(DeviceTreeVseDevice vseDevice, AggregationFunction aggregationFunction, int poolingGrid)
        => Identifiers.GetVseVariantIdentifier(GetVseAddressWithPort(vseDevice.Url), GetAggregationFunctionString(aggregationFunction, poolingGrid));
}
