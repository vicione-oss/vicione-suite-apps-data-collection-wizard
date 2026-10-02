using DataCollectionWizard.Internal.Contracts;
using DataCollectionWizard.Internal.Services.DeviceDataflowGenerators;
using ViciOne.Cluster.Builder;
using ViciOne.Cluster.Builder.Extensions;
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
    // Tree node design ids, which the MQTT (Mqtt.yaml) and OPC-UA Server (OpcUaServer.yaml) DataPorts share.
    protected const string PortDesignIdFolder = "Folder";
    private const string PortDesignIdDataPointBool = "DataPointBool";
    private const string PortDesignIdDataPointFloat = "DataPointFloat";
    private const string PortDesignIdDataPointInteger = "DataPointInteger";
    private const string PortDesignIdDataPointString = "DataPointString";

    /// <summary>
    /// Sanitizes a device tree node name so it is valid as a DataPort tree node name for the target protocol.
    /// </summary>
    private protected abstract string GetSafeNodeName(string name);

    /// <summary>
    /// The longest DataPort tree node name the target protocol accepts, or null if it has no limit.
    /// </summary>
    private protected virtual int? MaxNodeNameLength => null;

    /// <summary>
    /// Returns <paramref name="name"/>, or <paramref name="name"/> with a "_2", "_3", ... suffix if a sibling
    /// already uses it. Different device tree names can sanitize to the same node name (e.g. "Temp.1" and
    /// "Temp_1"), and two siblings sharing a name would publish over each other. With a length limit the name
    /// is shortened to make room for the suffix, so truncation cannot cut it off again.
    /// </summary>
    private string GetUniqueSiblingName(string name, HashSet<string> siblingNames)
    {
        var uniqueName = name;

        for (var suffix = 2; !siblingNames.Add(uniqueName); suffix++)
        {
            var suffixText = $"_{suffix}";
            var baseName = MaxNodeNameLength is { } maxLength && name.Length + suffixText.Length > maxLength
                ? name[..(maxLength - suffixText.Length)]
                : name;

            uniqueName = baseName + suffixText;
        }

        return uniqueName;
    }

    internal static TreeModel? BuildLoggedTreeRecursively(IDeviceTreeBase node, IEnumerable<string> loggedNodeIds, List<ProcessDataConfiguration> loggedProcessDataNodes)
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

        if (children.Count != 0)
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

    /// <param name="addDataPointChildren">
    /// Adds protocol-specific children below each created data point node, given the device output the data point is fed from.
    /// </param>
    internal void BuildDataportNodesRecursively(List<TreeModel> children, DataPort dataPort, DataPortTreeNode? parent, ClusterBuilder builder,
                                                Dictionary<string, DataOutputInfo> dataOutputs, Dictionary<string, AggregationFunctionCloudInputs> result,
                                                Action<DataPortTreeNode, DataOutputInfo?>? addDataPointChildren = null)
    {
        var siblingNames = new HashSet<string>(StringComparer.Ordinal);

        foreach (var child in children)
        {
            var (dataportNodeDesignId, dataportNodeValueType, dataportNodeTransferMode) = GetDataPortNodeKind(child, builder, dataOutputs);
            var dataportNodeName = GetUniqueSiblingName(GetSafeNodeName(child.Name), siblingNames);
            DataPortTreeNode childNode;

            if (parent is null)
            {
                childNode = builder.Editors.DataPort.AddTreeNode(dataportNodeDesignId, dataPort, dataportNodeName, dataportNodeValueType, dataportNodeTransferMode);
            }
            else
            {
                childNode = builder.Editors.DataPortTreeNode.AddTreeNode(dataportNodeDesignId, parent, dataportNodeName, dataportNodeValueType, dataportNodeTransferMode);
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

                addDataPointChildren?.Invoke(childNode, dataOutputs.GetValueOrDefault(child.DataConfig.Node.Id));
            }

            BuildDataportNodesRecursively(child.Children, dataPort, childNode, builder, dataOutputs, result, addDataPointChildren);
        }
    }

    private static (string DesignId, Type? ValueType, DataPortTransferMode TransferMode) GetDataPortNodeKind(TreeModel child, ClusterBuilder builder,
                                                                                                               Dictionary<string, DataOutputInfo> dataOutputs)
    {
        if (child.DataConfig is null)
        {
            return (PortDesignIdFolder, null, DataPortTransferMode.None);
        }

        var dataType = child.DataConfig.Node.DataType;

        if (dataType == DataType.Text)
        {
            return (PortDesignIdDataPointString, typeof(string), DataPortTransferMode.OnChange);
        }

        if (dataType is not (DataType.UnsignedWhole or DataType.Whole or DataType.Real or DataType.Flag))
        {
            throw new NotSupportedException($"Data type {dataType} is not supported.");
        }

        // Compressed values leave the compressor as double. OnChange values are connected straight from the
        // device output without conversion, so the node has to match the output's own type (bool, long, ...).
        if (child.DataConfig.Configuration.CompressionTime != (int)AggregationInterval.OnChange
            || !dataOutputs.TryGetValue(child.DataConfig.Node.Id, out var dataOutput))
        {
            return (PortDesignIdDataPointFloat, typeof(double), DataPortTransferMode.OnChange);
        }

        var outputType = builder.DetermineValueType(dataOutput.Output);

        if (outputType == typeof(bool))
        {
            return (PortDesignIdDataPointBool, typeof(bool), DataPortTransferMode.OnChange);
        }

        if (outputType == typeof(long))
        {
            return (PortDesignIdDataPointInteger, typeof(long), DataPortTransferMode.OnChange);
        }

        if (outputType == typeof(double) || outputType == typeof(float))
        {
            return (PortDesignIdDataPointFloat, typeof(double), DataPortTransferMode.OnChange);
        }

        throw new NotSupportedException($"Output type {outputType} of {child.DataConfig.Node.Id} is not supported for on change transfer.");
    }
}
