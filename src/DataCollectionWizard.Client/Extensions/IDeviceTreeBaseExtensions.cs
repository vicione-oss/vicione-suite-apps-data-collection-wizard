using DataCollectionWizard.Client.Models;
using DataCollectionWizard.Public.Extensions;
using ViciOne.Driver.IoTCore.Contracts.DeviceTree;

namespace DataCollectionWizard.Client.Extensions;

internal static class IDeviceTreeBaseExtensions
{
    public static string GetBreadcrumb(this IEnumerable<IDeviceTreeBase> pathToNode)
    {
        var resultBreadcrumbs = new List<string>();

        foreach (var node in pathToNode)
            resultBreadcrumbs.Add(GetBreadcrumbDisplayText(node));

        return string.Join(" / ", resultBreadcrumbs);
    }

    public static string GetBreadcrumbDisplayText(this IDeviceTreeBase deviceTreeNode)
    {
        if (deviceTreeNode is IDeviceTreeAliasNode aliasNode && !string.IsNullOrWhiteSpace(aliasNode.NameAlias))
            return $"{aliasNode.NameAlias}";

        if (deviceTreeNode is IAliasStructureNode aliasStructureNode)
            return $"{aliasStructureNode.Alias}";

        return $"{deviceTreeNode.Name}";
    }

    public static NodeStatus GetStatus(this IDeviceTreeBase device)
    {
        var deviceStatus = NodeStatus.None;

        if (device.IsOffline)
            deviceStatus |= NodeStatus.Offline;

        if (device.IsNew)
            deviceStatus |= NodeStatus.New;

        if (!device.IsSupported())
            deviceStatus |= NodeStatus.NotSupported;

        if (device is DeviceTreeDevice deviceTreeDevice && deviceTreeDevice.IsUnknown)
            deviceStatus |= NodeStatus.Unknown;

        return deviceStatus;
    }

    private static bool IsSupported(this IDeviceTreeBase device)
    {
        if (device is not IDeviceTreeDataNode dataNodeDevice)
            return true;

        return dataNodeDevice.DataType.SupportedForLogging();
    }
}
