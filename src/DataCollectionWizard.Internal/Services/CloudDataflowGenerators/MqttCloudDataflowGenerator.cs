using DataCollectionWizard.Internal.Contracts;
using DataCollectionWizard.Internal.Services.DesignIds;
using DataCollectionWizard.Internal.Services.DeviceDataflowGenerators;
using Sdk.Connections.Contracts;
using Sdk.Connections.Extensions;
using Sdk.Instance;
using ViciOne.Cluster.Builder;
using ViciOne.Cluster.Builder.Extensions;
using ViciOne.Cluster.Model;
using ViciOne.DeviceTree.Contracts;

namespace DataCollectionWizard.Internal.Services.CloudDataflowGenerators;

public sealed class MqttCloudDataflowGenerator(IInstanceInformationProvider instanceInformationProvider) : CloudDataflowTreeGenerator, ICloudDataflowGenerator
{
    private const string PortDesignIdMqttDataPointBool = "DataPointBool";
    private const string PortDesignIdMqttDataPointFloat = "DataPointFloat";
    private const string PortDesignIdMqttDataPointInteger = "DataPointInteger";
    private const string PortDesignIdMqttDataPointString = "DataPointString";
    private const string PortDesignIdMqttFolder = "Folder";

    public string Name => "mqtt";

    protected override string PortDesignIdFolder => "Folder";
    protected override string PortDesignIdDataPointDouble => "DataPointDouble";
    protected override string PortDesignIdDataPointString => "DataPointString";

    public Dictionary<string, AggregationFunctionCloudInputs> GenerateCloudDataflow(Connection connection,
                                                                            IDeviceTreeMasterNode deviceTreeMaster,
                                                                            ClusterBuilder builder,
                                                                            Dataflow dataflow,
                                                                            string machineIdentifier,
                                                                            Dictionary<string, DataOutputInfo> dataOutputs,
                                                                            uint engineCycleInterval,
                                                                            ChildContainer cloudContainer,
                                                                            Dictionary<string, RotationalFrequencyOutputs> rotationalFrequencyOutputs,
                                                                            List<ProcessDataConfiguration> loggedProcessDataNodes,
                                                                            List<IDeviceTreeDataNode> loggedRawDataNodes)
    {
        if (!MqttCloudFilter.IsMqttConnection(connection))
        {
            throw new ArgumentException("Invalid connection type", nameof(connection));
        }

        var result = new Dictionary<string, AggregationFunctionCloudInputs>();
        var loggedNodeIds = loggedProcessDataNodes.Select(n => n.Node.Id).ToHashSet();
        var loggedTree = BuildLoggedTreeRecursively(deviceTreeMaster, loggedNodeIds, loggedProcessDataNodes);

        if (loggedTree is null)
            return result;

        var dataport = GenerateDataPort(connection, deviceTreeMaster, builder, dataflow);

        var edgeNode = builder.Editors.DataPort.AddTreeNode(PortDesignIdFolder, dataport, GetSafeNodeName(instanceInformationProvider.Local.Name ?? instanceInformationProvider.Local.SerialNumber), null, DataPortTransferMode.None);
        var deviceNode = builder.Editors.DataPortTreeNode.AddTreeNode(PortDesignIdFolder, edgeNode, GetSafeNodeName(deviceTreeMaster.Url.DnsSafeHost), null, DataPortTransferMode.None);

        BuildDataportNodesRecursively(loggedTree!.Children, dataport, deviceNode, builder, dataOutputs, result);

        return result;
    }

    private protected override string GetSafeNodeName(string name)
        // Replace any characters that are not allowed in MQTT topic names with underscores
        => new([.. name.Select(c => char.IsLetterOrDigit(c) || c == '-' || c == '_' ? c : '_')]);

    /// <summary>
    /// Returns <paramref name="name"/>, or <paramref name="name"/> with a "_2", "_3", ... suffix if a sibling
    /// already uses it. Different device tree names can sanitize to the same topic level (e.g. "Temp.1" and
    /// "Temp_1"), and two siblings sharing a topic would publish over each other.
    /// </summary>
    private static string GetUniqueSiblingName(string name, HashSet<string> siblingNames)
    {
        var uniqueName = name;

        for (var suffix = 2; !siblingNames.Add(uniqueName); suffix++)
        {
            uniqueName = $"{name}_{suffix}";
        }

        return uniqueName;
    }

    internal static void BuildDataportNodesRecursively(List<TreeModel> children, DataPort dataPort, DataPortTreeNode? parent, ClusterBuilder builder,
                                                       Dictionary<string, DataOutputInfo> dataOutputs, Dictionary<string, AggregationFunctionCloudInputs> result)
    {
        var siblingNames = new HashSet<string>(StringComparer.Ordinal);

        foreach (var child in children)
        {
            var (dataportNodeDesignId, dataportNodeValueType) = GetDataPortNodeDesign(child, builder, dataOutputs);
            var dataportNodeTransferMode = GetDataPortNodeTransferMode(child);
            var dataportNodeName = GetUniqueSiblingName(GetMqttSafeTopicName(child.Name), siblingNames);
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
            }

            BuildDataportNodesRecursively(child.Children, dataPort, childNode, builder, dataOutputs, result);
        }
    }

    private static DataPortTransferMode GetDataPortNodeTransferMode(TreeModel child)
    {
        if (child.DataConfig is null)
        {
            return DataPortTransferMode.None;
        }

        return DataPortTransferMode.OnChange;
    }

    private static (string DesignId, Type? ValueType) GetDataPortNodeDesign(TreeModel child, ClusterBuilder builder, Dictionary<string, DataOutputInfo> dataOutputs)
    {
        if (child.DataConfig is null)
        {
            return (PortDesignIdMqttFolder, null);
        }

        var dataType = child.DataConfig.Node.DataType;

        if (dataType == DataType.Text)
        {
            return (PortDesignIdMqttDataPointString, typeof(string));
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
            return (PortDesignIdMqttDataPointFloat, typeof(double));
        }

        var outputType = builder.DetermineValueType(dataOutput.Output);

        if (outputType == typeof(bool))
        {
            return (PortDesignIdMqttDataPointBool, typeof(bool));
        }

        if (outputType == typeof(long))
        {
            return (PortDesignIdMqttDataPointInteger, typeof(long));
        }

        if (outputType == typeof(double) || outputType == typeof(float))
        {
            return (PortDesignIdMqttDataPointFloat, typeof(double));
        }

        throw new NotSupportedException($"Output type {outputType} of {child.DataConfig.Node.Id} is not supported for on change transfer.");
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

    private static DataPort GenerateDataPort(Connection connection, IDeviceTreeMasterNode deviceTreeMaster, ClusterBuilder builder, Dataflow dataflow)
    {
        var dataPort = builder.Editors.Dataflow.AddDataPort(dataflow, FunctionBlocks.MqttDataPort.DesignId,
                                    $"{connection.Name} - {deviceTreeMaster.Url}", DataPortDirection.Out, FunctionBlocks.MqttDataPort.Type);

        MqttDataPortProperties.Add(builder, dataPort, connection.GetMqttConnection()!);

        return dataPort;
    }
}
