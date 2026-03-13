using ViciOne.Cluster.Builder;
using ViciOne.Cluster.Model;
using ViciOne.Driver.IoTCore.Contracts.DeviceTree;
using ViciOne.Driver.IoTCore.Contracts.DeviceTree.Extensions;

namespace DataCollectionWizard.Internal.Services;

public class DeviceContainerManager
{
    private readonly Dictionary<string, IDeviceTreeBase?> _nodesParents;
    private readonly Container _parentContainer;
    private readonly ClusterBuilder _clusterBuilder;
    private readonly Dictionary<string, Container> _nodesContainers = new();
    private readonly Dictionary<Type, Func<IDeviceTreeBase, string>> _nameGenerators = new();

    public DeviceContainerManager(IDeviceTreeMasterNode device, Container container, ClusterBuilder builder)
    {
        var allNodes = device.GetNodeAndDescendants().ToArray();
        _nodesParents = allNodes.ToDictionary(n => n.Id, n => allNodes.FirstOrDefault(p => p.Children.Contains(n)));
        _parentContainer = container;
        _clusterBuilder = builder;
    }

    private Container GetNodeContainer(IDeviceTreeBase node)
    {

        if (_nodesContainers.TryGetValue(node.Id, out var cachedContainer))
            return cachedContainer;

        var parentNode = _nodesParents[node.Id];

        if (parentNode is null)
            return _parentContainer;

        var parentContainer = GetNodeContainer(parentNode);
        var name = node.Name;

        if (_nameGenerators.TryGetValue(parentNode.GetType(), out var nameGenerator))
            name = nameGenerator(parentNode);

        var newContainer = _clusterBuilder.Editors.Container.AddContainer(parentContainer, name);
        _nodesContainers[node.Id] = newContainer;

        return newContainer;
    }

    public Container GetParentContainer(IDeviceTreeBase node)
    {
        var parentNode = _nodesParents[node.Id];

        if (parentNode is null)
            return _parentContainer;

        return GetNodeContainer(parentNode);
    }

    public void AddNameGeneration<T>(Func<T, string> getName)
    {
        _nameGenerators.Add(typeof(T), (IDeviceTreeBase node) => getName((T)node));
    }
}
