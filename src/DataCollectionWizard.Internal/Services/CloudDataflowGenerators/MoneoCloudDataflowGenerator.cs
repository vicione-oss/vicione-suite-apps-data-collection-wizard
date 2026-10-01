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
using ViciOne.DeviceTree.Contracts;

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

    public Dictionary<string, AggregationFunctionCloudInputs> GenerateCloudDataflow(Connection connection, IDeviceTreeMasterNode deviceTreeMaster,
                                                                            ClusterBuilder builder, Dataflow dataflow, string machineIdentifier,
                                                                            Dictionary<string, DataOutputInfo> dataOutputs, uint engineCycleInterval,
                                                                            ChildContainer cloudContainer,
                                                                            Dictionary<string, RotationalFrequencyOutputs> rotationalFrequencyOutputs,
                                                                            List<ProcessDataConfiguration> loggedProcessDataNodes,
                                                                            List<IDeviceTreeDataNode> loggedRawDataNodes)
    {
        if (!MoneoCloudFilter.IsMoneoConnection(connection))
        {
            throw new ArgumentException("Invalid connection type", nameof(connection));
        }

        var result = new Dictionary<string, AggregationFunctionCloudInputs>();

        var deviceContainerManager = new DeviceContainerManager(deviceTreeMaster, () => cloudContainer, dataflow, builder);

        var stringOutput = GetConstantStringOutput(builder, dataflow, cloudContainer);
        GenerateDataPort(connection, deviceTreeMaster, builder, dataflow, stringOutput, out var deviceId, out var deviceIdNode);
        GenerateProcessData(builder, dataflow, engineCycleInterval, loggedProcessDataNodes, deviceId, deviceIdNode, deviceContainerManager, dataOutputs, result);

        return result;
    }

    private static void GenerateProcessData(ClusterBuilder builder, Dataflow dataflow, uint engineCycleInterval,
                                            List<ProcessDataConfiguration> loggedProcessDataNodes,
                                            string deviceId, DataPortTreeNode deviceIdNode, DeviceContainerManager containerManager,
                                            Dictionary<string, DataOutputInfo> dataOutputs, Dictionary<string, AggregationFunctionCloudInputs> result)
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

            var valueInput = dataFormatterFb.GetInputByDesignId(FunctionBlocks.DataFormatter.Inputs.Value);

            result[currentProcessDataNode.Node.Id] = new AggregationFunctionCloudInputs()
            {
                Avg = new CloudInput() { InputConnector = valueInput },
                Last = new CloudInput() { InputConnector = valueInput },
                Max = new CloudInput() { InputConnector = valueInput },
                Min = new CloudInput() { InputConnector = valueInput },
                Value = new CloudInput() { InputConnector = valueInput },
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

        MqttDataPortProperties.Add(builder, dataPort, connection.GetMqttConnection()!);

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
