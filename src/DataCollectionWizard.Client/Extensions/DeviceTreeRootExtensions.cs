using ViciOne.DeviceTree.Contracts;

namespace DataCollectionWizard.Client.Extensions;

internal static class DeviceTreeRootExtensions
{
    public static Dictionary<IDeviceTreeBase, List<IDeviceTreeBase>> GetNodePaths(this DeviceTreeRoot deviceTree)
        => new List<DeviceTreeRoot> { deviceTree }.GetNodePaths();

    public static Dictionary<IDeviceTreeBase, List<IDeviceTreeBase>> GetNodePaths(this IEnumerable<DeviceTreeRoot> deviceTreeList)
    {
        var result = new List<KeyValuePair<IDeviceTreeBase, List<IDeviceTreeBase>>>();
        foreach (var deviceTree in deviceTreeList)
        {
            var deviceTreePaths = new Dictionary<IDeviceTreeBase, List<IDeviceTreeBase>>();
            GetNodePathsRecursively(deviceTree, deviceTreePaths, []);
            result.AddRange([.. deviceTreePaths]);
        }

        return result.ToDictionary(r => r.Key, r => r.Value);

        static void GetNodePathsRecursively(IDeviceTreeBase node, Dictionary<IDeviceTreeBase, List<IDeviceTreeBase>> deviceTreePaths, List<IDeviceTreeBase> path)
        {
            var newPath = new List<IDeviceTreeBase>(path) { node };
            deviceTreePaths[node] = path;

            foreach (var child in node.Children)
                GetNodePathsRecursively(child, deviceTreePaths, newPath);
        }
    }
}
