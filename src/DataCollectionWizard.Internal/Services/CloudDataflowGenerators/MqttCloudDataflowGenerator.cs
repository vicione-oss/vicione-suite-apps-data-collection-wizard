using DataCollectionWizard.Internal.Services.DesignIds;
using DataCollectionWizard.Internal.Services.DeviceDataflowGenerators;
using Sdk.Connections.Contracts;
using Sdk.Connections.Extensions;
using Sdk.Instance;
using ViciOne.Cluster.Builder;
using ViciOne.Cluster.Model;
using ViciOne.DeviceTree.Contracts;

namespace DataCollectionWizard.Internal.Services.CloudDataflowGenerators;

public class MqttCloudDataflowGenerator(IInstanceInformationProvider instanceInformationProvider) : ICloudDataflowGenerator
{
    private const string PortDesignIdMqttDataPointFloat = "DataPointFloat";
    private const string PortDesignIdMqttDataPointString = "DataPointString";
    private const string PortDesignIdMqttFolder = "Folder";

    public string Name => "mqtt";

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

        var edgeNode = builder.Editors.DataPort.AddTreeNode(PortDesignIdMqttFolder, dataport, GetMqttSafeTopicName(instanceInformationProvider.Local.Name ?? instanceInformationProvider.Local.SerialNumber), null, DataPortTransferMode.None);
        var deviceNode = builder.Editors.DataPortTreeNode.AddTreeNode(PortDesignIdMqttFolder, edgeNode, GetMqttSafeTopicName(deviceTreeMaster.Url.DnsSafeHost), null, DataPortTransferMode.None);

        BuildDataportNodesRecursively(loggedTree!.Children, dataport, deviceNode, builder, result);

        return result;
    }

    private static string GetMqttSafeTopicName(string name)
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

    internal static void BuildDataportNodesRecursively(List<TreeModel> children, DataPort dataPort, DataPortTreeNode? parent, ClusterBuilder builder, Dictionary<string, AggregationFunctionCloudInputs> result)
    {
        var siblingNames = new HashSet<string>(StringComparer.Ordinal);

        foreach (var child in children)
        {
            var dataportNodeDesignId = GetDataPortNodeDesignId(child);
            var dataportNodeTransferMode = GetDataPortNodeTransferMode(child);
            var dataportNodeValueType = GetDataportNodeValueType(child);
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

            BuildDataportNodesRecursively(child.Children, dataPort, childNode, builder, result);
        }
    }

    private static Type? GetDataportNodeValueType(TreeModel child)
    {
        if (child.DataConfig is null)
        {
            return null;
        }

        return child.DataConfig.Node.DataType switch
        {
            DataType.UnsignedWhole or DataType.Whole or DataType.Real or DataType.Flag => typeof(double),
            DataType.Text => typeof(string),
            _ => throw new NotSupportedException($"Data type {child.DataConfig.Node.DataType} is not supported."),
        };
    }

    private static DataPortTransferMode GetDataPortNodeTransferMode(TreeModel child)
    {
        if (child.DataConfig is null)
        {
            return DataPortTransferMode.None;
        }

        return DataPortTransferMode.OnChange;
    }

    private static string GetDataPortNodeDesignId(TreeModel child)
    {
        if (child.DataConfig is null)
        {
            return PortDesignIdMqttFolder;
        }

        return child.DataConfig.Node.DataType switch
        {
            DataType.UnsignedWhole or DataType.Whole or DataType.Real or DataType.Flag => PortDesignIdMqttDataPointFloat,
            DataType.Text => PortDesignIdMqttDataPointString,
            _ => throw new NotSupportedException($"Data type {child.DataConfig.Node.DataType} is not supported."),
        };
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

        var mqttConnection = connection.GetMqttConnection()!;

        builder.Editors.DataPort.AddProperty("ClientId", dataPort, null, mqttConnection.ClientId);
        builder.Editors.DataPort.AddProperty("WillTopic", dataPort, null, mqttConnection.WillTopic);
        builder.Editors.DataPort.AddProperty("WillMessage", dataPort, null, mqttConnection.WillMessage);
        builder.Editors.DataPort.AddProperty("WillRetain", dataPort, null, mqttConnection.WillRetain);
        builder.Editors.DataPort.AddProperty("Protocol", dataPort, null, (byte)mqttConnection.Protocol);
        builder.Editors.DataPort.AddProperty("Host", dataPort, null, mqttConnection.Address);
        builder.Editors.DataPort.AddProperty("Port", dataPort, null, (ushort?)mqttConnection.Port);
        builder.Editors.DataPort.AddProperty("ProtocolVersion", dataPort, null, (byte)1);
        builder.Editors.DataPort.AddProperty("CertificateFile", dataPort, null, mqttConnection.ClientCertificate);
        builder.Editors.DataPort.AddProperty("CertificatePrivateKeyFile", dataPort, null, mqttConnection.ClientCertificateKey);
        builder.Editors.DataPort.AddProperty("CleanSession", dataPort, null, mqttConnection.CleanSession);
        builder.Editors.DataPort.AddProperty("DisableCertificateValidation", dataPort, null, true);

        builder.Editors.DataPort.AddProperty("Pooling", dataPort, null, true);
        builder.Editors.DataPort.AddProperty("MaxPendingMessages", dataPort, null, 10000);
        builder.Editors.DataPort.AddProperty("QualityOfService", dataPort, null, (byte)1);
        builder.Editors.DataPort.AddProperty("BrokerReceiveMaximum", dataPort, null, (ushort)100);

        builder.Editors.DataPort.AddProperty("Username", dataPort, null, mqttConnection.Username);
        builder.Editors.DataPort.AddProperty("Password", dataPort, null, mqttConnection.Password);

        return dataPort;
    }
}
