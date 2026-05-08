using System.Drawing;
using DataCollectionWizard.Internal.Extensions;
using DataCollectionWizard.Internal.Services.DesignIds;
using DataCollectionWizard.Internal.Services.DeviceDataflowGenerators;
using DataCollectionWizard.Public;
using DataCollectionWizard.Public.Extensions;
using Sdk.Connections.Contracts;
using Sdk.Connections.Extensions;
using ViciOne.Cluster.Builder;
using ViciOne.Cluster.Model;
using ViciOne.Driver.IoTCore.Contracts.DeviceTree;

namespace DataCollectionWizard.Internal.Services.CloudDataflowGenerators;

public sealed class MoneoCloudDataflowGenerator : ICloudDataflowGenerator
{
    private const string ConstantStringStatusFbName = "Moneo Connect Constant Status";
    private const string MoneoConnectStatus = """{"connectionStatus":"Connected", "heartbeatIntervalSeconds": 20}""";
    private const string PortDesignIdMqttDataPointFloat = "DataPointFloat";
    private const string PortDesignIdMqttDataPointString = "DataPointString";
    private const string PortDesignIdMqttFolder = "Folder";

    public string Name => "moneo";

    private static FunctionBlock AddDataFormatterFb(ClusterBuilder builder, Dataflow dataflow, DeviceContainerManager deviceContainerManager, string fbName,
        string thingId, string processId, uint compressionTime, uint engineCycleInterval, IDeviceTreeBase node)
    {
        var dataFormatterFb = builder.Editors.Container.AddSubFunctionBlock(dataflow, FunctionBlocks.DataFormatter.DesignId, fbName,
            deviceContainerManager.GetParentContainer(node), 0, FunctionBlocks.DefaultVerticalSeparation);

        var cycleFrequency = compressionTime / engineCycleInterval;
        builder.Editors.FunctionBlock.SetRunMode(dataFormatterFb, FunctionBlockRunMode.Cyclic);
        builder.Editors.FunctionBlock.SetCycleFrequency(dataFormatterFb, cycleFrequency);

        builder.Editors.Setting.SetFunctionBlockSetting(dataFormatterFb, FunctionBlocks.DataFormatter.Settings.ThingId, thingId);
        builder.Editors.Setting.SetFunctionBlockSetting(dataFormatterFb, FunctionBlocks.DataFormatter.Settings.ProcessId, processId);

        return dataFormatterFb;
    }

    private static void ConnectDataFormatterToMoneoConnectDataPort(ClusterBuilder builder, FunctionBlock dataFormatterFb, DataPortTreeNode processDataIdNode)
        => builder.Editors.DataPortTreeNode.AssignConnector(processDataIdNode, dataFormatterFb.GetOutputByDesignId(FunctionBlocks.DataFormatter.Outputs.FormattedValue));

    private static DataPortTreeNode CreateMoneoDataPortTreeNode(ClusterBuilder builder, DataPortTreeNode parent, string processId)
        => builder.Editors.DataPortTreeNode.AddTreeNode(PortDesignIdMqttDataPointFloat, parent, processId, typeof(object), DataPortTransferMode.OnChange);

    public Dictionary<string, PoolingModesCloudInput> GenerateCloudDataflow(Connection connection, IDeviceTreeMasterNode deviceTreeMaster,
                                                                            ClusterBuilder builder, Dataflow dataflow, string machineIdentifier,
                                                                            Dictionary<string, DataOutputInfo> dataOutputs, uint engineCycleInterval,
                                                                            ChildContainer cloudContainer,
                                                                            Dictionary<string, RotationalFrequencyOutputs> rotationalFrequencyOutputs,
                                                                            List<ProcessDataConfiguration> loggedProcessDataNodes,
                                                                            List<IDeviceTreeDataNode> loggedRawDataNodes)
    {
        var result = new Dictionary<string, PoolingModesCloudInput>();

        var deviceContainerManager = new DeviceContainerManager(deviceTreeMaster, cloudContainer, builder);

        var stringOutput = GetConstantStringOutput(builder, dataflow, cloudContainer);
        GenerateDataPort(connection, deviceTreeMaster, builder, dataflow, stringOutput, out var deviceId, out var deviceIdNode);
        GenerateProcessData(builder, dataflow, engineCycleInterval, loggedProcessDataNodes, result, deviceId, deviceIdNode, deviceContainerManager, dataOutputs);

        return result;
    }

    private static void GenerateProcessData(ClusterBuilder builder, Dataflow dataflow, uint engineCycleInterval,
                                            List<ProcessDataConfiguration> loggedProcessDataNodes, Dictionary<string, PoolingModesCloudInput> result,
                                            string deviceId, DataPortTreeNode deviceIdNode, DeviceContainerManager containerManager,
                                            Dictionary<string, DataOutputInfo> dataOutputs)
    {
        foreach (var currentProcessDataNode in loggedProcessDataNodes)
        {
            var compressionTime = (uint)currentProcessDataNode.Configuration.CompressionTime;
            var processId = MoneoUtils.GenerateDataSourceId(currentProcessDataNode.Node.Id);
            var processDataIdNode = CreateMoneoDataPortTreeNode(builder, deviceIdNode, processId);

            var dataFormatterFb = AddDataFormatterFb(builder, dataflow, containerManager, currentProcessDataNode.Node.Name, deviceId,
                processId, compressionTime, engineCycleInterval, currentProcessDataNode.Node);
            ConnectDataFormatterToMoneoConnectDataPort(builder, dataFormatterFb, processDataIdNode);

            if (dataOutputs.TryGetValue(currentProcessDataNode.Node.Id, out var dataOutputInfo))
            {
                builder.Editors.Connector.AddLink(dataOutputInfo.AvailableOutput, dataFormatterFb.GetInputByDesignId(FunctionBlocks.DataFormatter.Inputs.Enabled), false);
            }
            else
            {
                throw new InvalidOperationException($"Did not find ProcessDataInfo for id {currentProcessDataNode.Node.Id}");
            }

            result[currentProcessDataNode.Node.Id] = new PoolingModesCloudInput()
            {
                Avg = new CloudInput() { InputConnector = dataFormatterFb.GetInputByDesignId(FunctionBlocks.DataFormatter.Inputs.Value) },
                Last = new CloudInput() { InputConnector = dataFormatterFb.GetInputByDesignId(FunctionBlocks.DataFormatter.Inputs.Value) },
                Max = new CloudInput() { InputConnector = dataFormatterFb.GetInputByDesignId(FunctionBlocks.DataFormatter.Inputs.Value) },
                Min = new CloudInput() { InputConnector = dataFormatterFb.GetInputByDesignId(FunctionBlocks.DataFormatter.Inputs.Value) },
                Value = new CloudInput() { InputConnector = dataFormatterFb.GetInputByDesignId(FunctionBlocks.DataFormatter.Inputs.Value) },
            };
        }
    }

    private static DataPort GenerateDataPort(Connection connection, IDeviceTreeMasterNode deviceTreeMaster, ClusterBuilder builder, Dataflow dataflow, ConnectorOutput stringOutput, out string deviceId, out DataPortTreeNode deviceIdNode)
    {
        var dataPort = builder.Editors.Dataflow.AddDataPort(dataflow, FunctionBlocks.MqttDataPort.DesignId,
                                    $"{connection.Name} - {deviceTreeMaster.Url}", DataPortDirection.Out, FunctionBlocks.MqttDataPort.Type);

        //v1/{region}/{tenant}/{instanceId}/processdata/{deviceId}/{datasourceId}

        connection.Metadata.TryGetValue("Region", out var region);
        connection.Metadata.TryGetValue("Tenant", out var tenant);
        connection.Metadata.TryGetValue("Instance", out var instanceId);

        var v1Node = builder.Editors.DataPort.AddTreeNode(PortDesignIdMqttFolder, dataPort, "v1", null, DataPortTransferMode.None);
        var regionNode = builder.Editors.DataPortTreeNode.AddTreeNode(PortDesignIdMqttFolder, v1Node, region ?? "unknown-region", null, DataPortTransferMode.None);
        var tenantNode = builder.Editors.DataPortTreeNode.AddTreeNode(PortDesignIdMqttFolder, regionNode, tenant ?? "unknown-tenant", null, DataPortTransferMode.None);
        var instanceIdNode = builder.Editors.DataPortTreeNode.AddTreeNode(PortDesignIdMqttFolder, tenantNode, instanceId ?? "unknown-instance", null, DataPortTransferMode.None);

        var deviceManagementNode = builder.Editors.DataPortTreeNode.AddTreeNode(PortDesignIdMqttFolder, instanceIdNode, "devicemanagement", null, DataPortTransferMode.None);
        var statusNode = builder.Editors.DataPortTreeNode.AddTreeNode(PortDesignIdMqttDataPointString, deviceManagementNode, "_status", typeof(string), DataPortTransferMode.OnChange);
        builder.Editors.DataPortTreeNode.AssignConnector(statusNode, stringOutput);
        builder.Editors.DataPortTreeNode.AddProperty("Serializer", statusNode, null, (byte)2);
        builder.Editors.DataPortTreeNode.SetTransferMode(statusNode, DataPortTransferMode.Periodic);
        builder.Editors.DataPortTreeNode.SetTransferIntervalInMs(statusNode, 20000);

        var processDataNode = builder.Editors.DataPortTreeNode.AddTreeNode(PortDesignIdMqttFolder, instanceIdNode, "processdata", null, DataPortTransferMode.None);

        deviceId = MoneoUtils.ConstructDeviceId(deviceTreeMaster.GetMacAddress(), MoneoUtils.GetFallbackIdentifier(deviceTreeMaster)).ToString();
        deviceIdNode = builder.Editors.DataPortTreeNode.AddTreeNode(PortDesignIdMqttFolder, processDataNode, deviceId, null, DataPortTransferMode.None);

        var moneoConnection = connection.GetMqttConnection()!;

        builder.Editors.DataPort.AddProperty("ClientId", dataPort, null, moneoConnection.ClientId);
        builder.Editors.DataPort.AddProperty("WillTopic", dataPort, null, moneoConnection.WillTopic);
        builder.Editors.DataPort.AddProperty("WillMessage", dataPort, null, moneoConnection.WillMessage);
        builder.Editors.DataPort.AddProperty("WillRetain", dataPort, null, moneoConnection.WillRetain);
        builder.Editors.DataPort.AddProperty("Protocol", dataPort, null, (byte)moneoConnection.Protocol);
        builder.Editors.DataPort.AddProperty("Host", dataPort, null, moneoConnection.Address);
        builder.Editors.DataPort.AddProperty("Port", dataPort, null, (ushort?)moneoConnection.Port);
        builder.Editors.DataPort.AddProperty("ProtocolVersion", dataPort, null, (byte)1);
        builder.Editors.DataPort.AddProperty("CertificateFile", dataPort, null, moneoConnection.ClientCertificate);
        builder.Editors.DataPort.AddProperty("CertificatePrivateKeyFile", dataPort, null, moneoConnection.ClientCertificateKey);
        builder.Editors.DataPort.AddProperty("CleanSession", dataPort, null, moneoConnection.CleanSession);
        builder.Editors.DataPort.AddProperty("DisableCertificateValidation", dataPort, null, true);

        builder.Editors.DataPort.AddProperty("Pooling", dataPort, null, true);
        builder.Editors.DataPort.AddProperty("MaxPendingMessages", dataPort, null, 10000);
        builder.Editors.DataPort.AddProperty("QualityOfService", dataPort, null, (byte)1);
        builder.Editors.DataPort.AddProperty("BrokerReceiveMaximum", dataPort, null, (ushort)100);

        builder.Editors.DataPort.AddProperty("Username", dataPort, null, moneoConnection.Username);
        builder.Editors.DataPort.AddProperty("Password", dataPort, null, moneoConnection.Password);

        return dataPort;
    }

    private static ConnectorOutput GetConstantStringOutput(ClusterBuilder builder, Dataflow dataflow, Container cloudContainer)
    {
        var constantStringFb = cloudContainer.FunctionBlocks.FirstOrDefault(fb => fb.DesignId == FunctionBlocks.ConstantString.DesignId && fb.Name == ConstantStringStatusFbName);

        if (constantStringFb is null)
        {
            constantStringFb = builder.Editors.Container.AddFunctionBlock(dataflow, FunctionBlocks.ConstantString.DesignId, ConstantStringStatusFbName,
                        cloudContainer, new Point(0, FunctionBlocks.DefaultVerticalSeparation * -1));

            builder.Editors.Setting.SetFunctionBlockSetting(constantStringFb, FunctionBlocks.ConstantString.Settings.DefaultValue, MoneoConnectStatus);
        }

        return constantStringFb.GetOutputByDesignId(FunctionBlocks.ConstantString.Outputs.Value);
    }
}
