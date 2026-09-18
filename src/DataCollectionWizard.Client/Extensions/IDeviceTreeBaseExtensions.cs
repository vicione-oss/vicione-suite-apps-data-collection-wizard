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

    // An aliased node is spelled "[Name] alias" here - the technical name leads, unlike the tree, where it
    // trails as a subtitle under the alias. Two reasons for the difference: a breadcrumb has no subtitle, so
    // leaving the name out drops it for good (a VSE object then appeared as its alias alone, and a group header
    // named something the rows under it spelled differently); and with the name in front, the segments of a path
    // start at a predictable place instead of each ending in a bracket at a different width. It also makes the
    // technical name searchable, which it was not before.
    public static string GetBreadcrumbDisplayText(this IDeviceTreeBase deviceTreeNode)
        => deviceTreeNode is IDeviceTreeAliasNode { Alias: var alias } && !string.IsNullOrWhiteSpace(alias)
            ? $"[{deviceTreeNode.Name}] {alias.Trim()}"
            : deviceTreeNode.Name;

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
