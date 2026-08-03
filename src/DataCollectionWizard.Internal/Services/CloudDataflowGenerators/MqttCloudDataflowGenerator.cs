using DataCollectionWizard.Internal.Services.DesignIds;
using DataCollectionWizard.Internal.Services.DeviceDataflowGenerators;
using Sdk.Connections.Contracts;
using Sdk.Connections.Extensions;
using Sdk.Instance;
using ViciOne.Cluster.Builder;
using ViciOne.Cluster.Model;
using ViciOne.Driver.IoTCore.Contracts.DeviceTree;

namespace DataCollectionWizard.Internal.Services.CloudDataflowGenerators;

public class MqttCloudDataflowGenerator(IInstanceInformationProvider instanceInformationProvider) : ICloudDataflowGenerator
{
    private const string PortDesignIdMqttDataPointFloat = "DataPointFloat";
    private const string PortDesignIdMqttDataPointString = "DataPointString";
    private const string PortDesignIdMqttFolder = "Folder";
    private const string ViciOneRootTopic = "vicione";

    public string Name => "mqtt";

    public Dictionary<string, PoolingModesCloudInput> GenerateCloudDataflow(Connection connection,
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
        var result = new Dictionary<string, PoolingModesCloudInput>();
        var loggedNodeIds = loggedProcessDataNodes.Select(n => n.Node.Id).ToHashSet();
        var loggedTree = BuildLoggedTreeRecursively(deviceTreeMaster, loggedNodeIds, loggedProcessDataNodes);

        if (loggedTree is null)
            return result;

        var dataport = GenerateDataPort(connection, deviceTreeMaster, builder, dataflow);

        var vicioneNode = builder.Editors.DataPort.AddTreeNode(PortDesignIdMqttFolder, dataport, ViciOneRootTopic, null, DataPortTransferMode.None);
        var edgeNode = builder.Editors.DataPortTreeNode.AddTreeNode(PortDesignIdMqttFolder, vicioneNode, instanceInformationProvider.Local.Name ?? instanceInformationProvider.Local.SerialNumber, null, DataPortTransferMode.None);
        var deviceNode = builder.Editors.DataPortTreeNode.AddTreeNode(PortDesignIdMqttFolder, edgeNode, GetMqttSafeTopicName(deviceTreeMaster.Url.DnsSafeHost), null, DataPortTransferMode.None);

        BuildDataportNodesRecursively(loggedTree!.Children, dataport, deviceNode, builder, result);

        return result;
    }

    private string GetMqttSafeTopicName(string dnsSafeHost)
        // Replace any characters that are not allowed in MQTT topic names with underscores
        => new string (dnsSafeHost.Select(c => char.IsLetterOrDigit(c) || c == '-' || c == '_' ? c : '_').ToArray());

    internal void BuildDataportNodesRecursively(List<TreeModel> children, DataPort dataPort, DataPortTreeNode? parent, ClusterBuilder builder, Dictionary<string, PoolingModesCloudInput> result)
    {
        foreach (var child in children)
        {
            var dataportNodeDesignId = GetDataPortNodeDesignId(child);
            var dataportNodeTransferMode = GetDataPortNodeTransferMode(child);
            var dataportNodeValueType = GetDataportNodeValueType(child);
            DataPortTreeNode childNode;

            if (parent is null)
            {
                childNode = builder.Editors.DataPort.AddTreeNode(dataportNodeDesignId, dataPort, GetMqttSafeTopicName(child.Name), dataportNodeValueType, dataportNodeTransferMode);
            }
            else
            {
                childNode = builder.Editors.DataPortTreeNode.AddTreeNode(dataportNodeDesignId, parent, GetMqttSafeTopicName(child.Name), dataportNodeValueType, dataportNodeTransferMode);
            }

            if (child.DataConfig is not null)
            {
                result[child.DataConfig.Node.Id] = new PoolingModesCloudInput()
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

    private Type? GetDataportNodeValueType(TreeModel child)
    {
        if (child.DataConfig is null)
        {
            return null;
        }

        switch (child.DataConfig.Node.DataType)
        {
            case DataType.BooleanT:
            case DataType.Float32T:
            case DataType.UIntegerT:
            case DataType.IntegerT:
                return typeof(float);
            case DataType.StringT:
            case DataType.OctetStringT:
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
            return PortDesignIdMqttFolder;
        }

        switch (child.DataConfig.Node.DataType)
        {
            case DataType.BooleanT:
            case DataType.Float32T:
            case DataType.UIntegerT:
            case DataType.IntegerT:
                return PortDesignIdMqttDataPointFloat;
            case DataType.StringT:
            case DataType.OctetStringT:
                return PortDesignIdMqttDataPointString;
            default:
                throw new NotSupportedException($"Data type {child.DataConfig.Node.DataType} is not supported.");
        }
    }

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
