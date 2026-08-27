using ViciOne.Cluster.Builder;
using ViciOne.Cluster.Model;
using ViciOne.DeviceTree.Contracts;

namespace DataCollectionWizard.Internal.Services.CloudDataflowGenerators;

/// <summary>
/// Base class for cloud dataflow generators that publish the logged device tree as a hierarchy of
/// DataPort tree nodes (folders and data points). Holds the tree-shaping logic shared by the MQTT and
/// OPC UA generators; derived classes only supply the protocol-specific node name sanitization.
/// </summary>
public abstract class CloudDataflowTreeGenerator
{
    protected abstract string PortDesignIdFolder { get; }
    protected abstract  string PortDesignIdDataPointFloat { get; }
    protected abstract string PortDesignIdDataPointString { get; }

    /// <summary>
    /// Sanitizes a device tree node name so it is valid as a DataPort tree node name for the target protocol.
    /// </summary>
    private protected abstract string GetSafeNodeName(string name);

    internal TreeModel? BuildLoggedTreeRecursively(IDeviceTreeBase node, IEnumerable<string> loggedNodeIds, List<ProcessDataConfiguration> loggedProcessDataNodes)
    {
        var children = new List<TreeModel>();

        foreach (var child in node.Children)
        {
            var loggedChild = BuildLoggedTreeRecursively(child, loggedNodeIds, loggedProcessDataNodes);

            if (loggedChild is not null)
            {
                children.Add(loggedChild);
            }
        }

        if (loggedNodeIds.Contains(node.Id))
        {
            return new TreeModel()
            {
                Children = children,
                DataConfig = loggedProcessDataNodes.FirstOrDefault(n => n.Node.Id == node.Id),
                Id = node.Id,
                Name = node.Name,
            };
        }

        if (children.Any())
        {
            return new TreeModel()
            {
                Children = children,
                Id = node.Id,
                Name = node.Name,
            };
        }

        return null;
    }

    internal void BuildDataportNodesRecursively(List<TreeModel> children, DataPort dataPort, DataPortTreeNode? parent, ClusterBuilder builder, Dictionary<string, AggregationFunctionCloudInputs> result)
    {
        foreach (var child in children)
        {
            var dataportNodeDesignId = GetDataPortNodeDesignId(child);
            var dataportNodeTransferMode = GetDataPortNodeTransferMode(child);
            var dataportNodeValueType = GetDataportNodeValueType(child);
            DataPortTreeNode childNode;

            if (parent is null)
            {
                childNode = builder.Editors.DataPort.AddTreeNode(dataportNodeDesignId, dataPort, GetSafeNodeName(child.Name), dataportNodeValueType, dataportNodeTransferMode);
            }
            else
            {
                childNode = builder.Editors.DataPortTreeNode.AddTreeNode(dataportNodeDesignId, parent, GetSafeNodeName(child.Name), dataportNodeValueType, dataportNodeTransferMode);
            }

            if (child.DataConfig is not null)
            {
                result[child.DataConfig.Node.Id] = new AggregationFunctionCloudInputs()
                {
                    Avg = new CloudInput() { InputTreeNode = childNode },
                    Last = new CloudInput() { InputTreeNode = childNode },
                    Max = new CloudInput() { InputTreeNode = childNode },
                    Min = new CloudInput() { InputTreeNode = childNode },
                    Value = new CloudInput() { InputTreeNode = childNode },
                };
            }

            BuildDataportNodesRecursively(child.Children, dataPort, childNode, builder, result);
        }
    }

    // Only Real/Text data points are wired up for now. The DataPorts also define
    // DataPointBool/Integer/DateTime/Binary node types, so this can be extended once those DataType
    // values are confirmed and their mapping to CLR types is settled.
    private Type? GetDataportNodeValueType(TreeModel child)
    {
        if (child.DataConfig is null)
        {
            return null;
        }

        switch (child.DataConfig.Node.DataType)
        {
            case DataType.Real:
                return typeof(float);
            case DataType.Text:
                return typeof(string);
            default:
                throw new NotSupportedException($"Data type {child.DataConfig.Node.DataType} is not supported.");
        }
    }

    private DataPortTransferMode GetDataPortNodeTransferMode(TreeModel child)
    {
        if (child.DataConfig is null)
        {
            return DataPortTransferMode.None;
        }

        return DataPortTransferMode.OnChange;
    }

    private string GetDataPortNodeDesignId(TreeModel child)
    {
        if (child.DataConfig is null)
        {
            return PortDesignIdFolder;
        }

        switch (child.DataConfig.Node.DataType)
        {
            case DataType.Real:
                return PortDesignIdDataPointFloat;
            case DataType.Text:
                return PortDesignIdDataPointString;
            default:
                throw new NotSupportedException($"Data type {child.DataConfig.Node.DataType} is not supported.");
        }
    }
}
