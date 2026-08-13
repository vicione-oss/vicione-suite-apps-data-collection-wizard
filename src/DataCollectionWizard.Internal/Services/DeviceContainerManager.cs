using DataCollectionWizard.Internal.Extensions;
using DataCollectionWizard.Internal.Services.DesignIds;
using ViciOne.Cluster.Builder;
using ViciOne.Cluster.Builder.Extensions;
using ViciOne.Cluster.Model;
using ViciOne.DeviceTree.Contracts;
using ViciOne.DeviceTree.Contracts.Extensions;

namespace DataCollectionWizard.Internal.Services;

internal sealed class DeviceContainerManager
{
    private const string InvalidNodeArgumentExceptionMessage = "Node has to be a sub node of this managers device at the moment of its creation.";
    private readonly ClusterBuilder _builder;
    private readonly Dataflow _dataflow;
    private readonly Dictionary<string, Container> _nodesContainers = [];
    private readonly Dictionary<string, IDeviceTreeBase?> _nodesParents;
    private readonly ChildContainer _parentContainer;

    public DeviceContainerManager(IDeviceTreeMasterNode device, ChildContainer parent, ClusterBuilder builder)
    {
        var allNodes = device.GetNodeAndDescendants().ToArray();
        _nodesParents = allNodes.ToDictionary(n => n.Id, n => allNodes.FirstOrDefault(p => p.Children.Contains(n)));
        _parentContainer = parent;
        _builder = builder;
        _dataflow = _builder.Cache.GetDataflow(_parentContainer);
    }

    private Container GetNodeContainer(IDeviceTreeBase node)
    {
        if (_nodesContainers.TryGetValue(node.Id, out var cachedContainer))
            return cachedContainer;

        if (!_nodesParents.TryGetValue(node.Id, out var parentNode))
            throw new ArgumentException(InvalidNodeArgumentExceptionMessage, nameof(node));

        if (parentNode is null)
            return _parentContainer;

        var parentContainer = GetNodeContainer(parentNode);

        var newContainer = _builder.Editors.Container.AddSubContainer(_dataflow, node.Name, parentContainer, 0, FunctionBlocks.DefaultVerticalSeparation);
        _nodesContainers[node.Id] = newContainer;

        return newContainer;
    }

    public Container GetParentContainer(IDeviceTreeBase node)
    {
        if (!_nodesParents.TryGetValue(node.Id, out var parentNode))
            throw new ArgumentException(InvalidNodeArgumentExceptionMessage, nameof(node));

        if (parentNode is null)
            return _parentContainer;

        return GetNodeContainer(parentNode);
    }
}
