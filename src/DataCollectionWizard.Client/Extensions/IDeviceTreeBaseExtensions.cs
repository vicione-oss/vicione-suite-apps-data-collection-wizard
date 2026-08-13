using DataCollectionWizard.Client.Models;
using DataCollectionWizard.Public.Extensions;
using ViciOne.DeviceTree.Contracts;

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
        if (deviceTreeNode is IDeviceTreeDeviceAliasNode deviceAliasNode)
            return $"{deviceAliasNode.Alias}";

        if (deviceTreeNode is IDeviceTreeUserAliasNode { Alias: var alias } && !string.IsNullOrWhiteSpace(alias))
            return $"{alias}";

        return $"{deviceTreeNode.Name}";
    }

    public static NodeStatus GetStatus(this IDeviceTreeBase device, bool isLiveView = false)
    {
        var deviceStatus = NodeStatus.None;

        if (device.Status != ConnectionStatus.Online)
            deviceStatus |= NodeStatus.Offline;

        if (device.IsNew)
            deviceStatus |= NodeStatus.New;

        if (!device.IsSupported(isLiveView))
            deviceStatus |= NodeStatus.NotSupported;

        if (device is DeviceTreeDevice deviceTreeDevice && deviceTreeDevice.IsUnknown)
            deviceStatus |= NodeStatus.Unknown;

        return deviceStatus;
    }

    private static bool IsSupported(this IDeviceTreeBase device, bool isLiveView)
    {
        if (device is not IDeviceTreeDataNode dataNodeDevice)
            return true;

        return isLiveView
            ? dataNodeDevice.DataType.SupportsLiveView
            : dataNodeDevice.DataType.SupportsLogging;
    }
}
