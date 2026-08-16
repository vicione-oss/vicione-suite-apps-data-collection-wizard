using DataCollectionWizard.Internal.Extensions;
using DataCollectionWizard.Internal.Services.DesignIds;
using DataCollectionWizard.Internal.Services.DeviceDataflowGenerators;
using Sdk.Connections.Contracts;
using Sdk.Connections.Extensions;
using ViciOne.Cluster.Builder;
using ViciOne.Cluster.Model;
using ViciOne.DeviceTree.Contracts;

namespace DataCollectionWizard.Internal.Services.CloudDataflowGenerators;

public sealed partial class AnnaCloudDataflowGenerator : ICloudDataflowGenerator
{
    private const ushort AnnaObjectDataQueueSize = 50_000;
    private const ushort AnnaRawDataQueueSize = 10;
    private const string FbNamePrefixAnnaRawData = "ANNA RawData Publisher";
    private const string PortDesignIdObjectDataNode = "ObjectData";
    private const string PortDesignIdRawDataNode = "RawData";

    public string Name => "ANNA";

    private static FunctionBlock AddAnnaObjectDataFb(ClusterBuilder builder, Dataflow dataflow, DeviceContainerManager containerManager, string datapointIdentifier, string name,
                                              CompressorConfiguration configuration,
                                              bool insertRefValue, bool insertRotSpeed, IDeviceTreeBase node)
    {
        var parentContainer = containerManager.GetParentContainer(node);

        var objectDataFb = builder.Editors.Container.AddSubFunctionBlock(dataflow, FunctionBlocks.AnnaObjectData.DesignId, name, parentContainer, 0, FunctionBlocks.DefaultVerticalSeparation + 20);
        var aggregationFunction = configuration.Aggregation;
        var isOnChange = configuration.CompressionTime == -1;
        var isMinMaxAvg = configuration.Aggregation == AggregationFunction.MinMaxAvg;

        builder.Editors.Setting.SetFunctionBlockSetting(objectDataFb, FunctionBlocks.AnnaObjectData.Settings.DatapointIdentifier, datapointIdentifier);
        builder.Editors.Setting.SetFunctionBlockSetting(objectDataFb, FunctionBlocks.AnnaObjectData.Settings.InsertAverage, (isMinMaxAvg || aggregationFunction == AggregationFunction.Avg) && !isOnChange);
        builder.Editors.Setting.SetFunctionBlockSetting(objectDataFb, FunctionBlocks.AnnaObjectData.Settings.InsertMaximum, (isMinMaxAvg || aggregationFunction == AggregationFunction.Max) && !isOnChange);
        builder.Editors.Setting.SetFunctionBlockSetting(objectDataFb, FunctionBlocks.AnnaObjectData.Settings.InsertMinimum, (isMinMaxAvg || aggregationFunction == AggregationFunction.Min) && !isOnChange);
        builder.Editors.Setting.SetFunctionBlockSetting(objectDataFb, FunctionBlocks.AnnaObjectData.Settings.InsertRefValue, insertRefValue && !isOnChange);
        builder.Editors.Setting.SetFunctionBlockSetting(objectDataFb, FunctionBlocks.AnnaObjectData.Settings.InsertRotSpeed, insertRotSpeed && !isOnChange);

        return objectDataFb;
    }

    private static FunctionBlock AddAnnaRawDataFb(ClusterBuilder builder, Dataflow dataflow, Container parentContainer, string fbName, string unit)
    {
        var rawDataFb = builder.Editors.Container.AddSubFunctionBlock(dataflow, FunctionBlocks.AnnaRawData.DesignId, fbName, parentContainer, FunctionBlocks.DefaultHorizontalSeparation, FunctionBlocks.DefaultVerticalSeparation);

        builder.Editors.Setting.SetFunctionBlockSetting(rawDataFb, FunctionBlocks.AnnaRawData.Settings.Unit, unit);
        return rawDataFb;
    }

    private static FunctionBlock GetOrAddAnnaRawDataFb(ClusterBuilder builder, Dataflow dataflow, string unit, Dictionary<string, FunctionBlock> rawDataFbsByUnit,
        IEnumerable<ConnectorOutput> rotationalFrequencyOutputs, string connectionName, DataPortTreeNode rawDataNode, Container cloudContainer)
    {
        var fbName = $"{FbNamePrefixAnnaRawData} {connectionName} {unit}";

        if (rawDataFbsByUnit.TryGetValue(unit, out var rawDataFb))
        {
            return rawDataFb;
        }

        var annaRawDataFb = AddAnnaRawDataFb(builder, dataflow, cloudContainer, fbName, unit);

        rawDataFbsByUnit[unit] = annaRawDataFb;

        var rotationalFrequenciesInput = annaRawDataFb.GetInputByDesignId(FunctionBlocks.AnnaRawData.Inputs.RotationalFrequencies);

        foreach (var rotationalFrequencyOutput in rotationalFrequencyOutputs)
            builder.Editors.Connector.AddLink(rotationalFrequencyOutput, rotationalFrequenciesInput, false);

        builder.Editors.DataPortTreeNode.AssignConnector(rawDataNode, annaRawDataFb.GetOutputByDesignId(FunctionBlocks.AnnaRawData.Outputs.Data));

        return annaRawDataFb;
    }

    public Dictionary<string, AggregationFunctionCloudInputs> GenerateCloudDataflow(Connection connection, IDeviceTreeMasterNode deviceTreeMaster, ClusterBuilder builder,
                                                                            Dataflow dataflow, string machineIdentifier, Dictionary<string, DataOutputInfo> dataOutputs, uint engineCycleInterval,
                                                                            ChildContainer cloudContainer, Dictionary<string, RotationalFrequencyOutputs> rotationalFrequencyOutputs,
                                                                            List<ProcessDataConfiguration> loggedProcessDataNodes, List<IDeviceTreeDataNode> loggedRawDataNodes)
    {
        var result = new Dictionary<string, AggregationFunctionCloudInputs>();

        if (!AnnaCloudFilter.IsAnnaConnection(connection))
        {
            throw new ArgumentException("Invalid connection type", nameof(connection));
        }

        GenerateDataPort(connection, deviceTreeMaster, builder, dataflow, machineIdentifier, out var objectDataNode, out var rawDataNode);
        GenerateObjectData(connection, builder, dataflow, loggedProcessDataNodes, dataOutputs, rotationalFrequencyOutputs, objectDataNode, cloudContainer, deviceTreeMaster, result);
        GenerateRawData(connection, builder, dataflow, loggedRawDataNodes, cloudContainer, rotationalFrequencyOutputs, rawDataNode, result);

        return result;
    }

    private static void GenerateDataPort(Connection connection, IDeviceTreeMasterNode deviceTreeMaster, ClusterBuilder builder, Dataflow dataflow, string machineIdentifier, out DataPortTreeNode objectDataNode, out DataPortTreeNode rawDataNode)
    {
        var annaConnection = connection.GetHttpConnection();
        ArgumentNullException.ThrowIfNull(annaConnection);

        var dataPort = builder.Editors.Dataflow.AddDataPort(dataflow, FunctionBlocks.AnnaDataPort.DesignId,
            $"{connection.Name} - {deviceTreeMaster.Url}", DataPortDirection.Out, FunctionBlocks.AnnaDataPort.Type);

        //Todo: Tatsächlichen Value Type setzen, dies ist ein Workaround für die Messe!!! (typeof(object))
        rawDataNode = builder.Editors.DataPort.AddTreeNode(PortDesignIdRawDataNode, dataPort, "RawData", typeof(object), DataPortTransferMode.OnChange);
        objectDataNode = builder.Editors.DataPort.AddTreeNode(PortDesignIdObjectDataNode, dataPort, "ObjectData", typeof(object), DataPortTransferMode.OnChange);

        var url = new Uri(annaConnection.BaseAddress);
        builder.Editors.DataPort.AddProperty("ApiKey", dataPort, null, annaConnection.ApiKey);
        builder.Editors.DataPort.AddProperty("Server", dataPort, null, $"{url.Scheme}{Uri.SchemeDelimiter}{url.DnsSafeHost}");
        builder.Editors.DataPort.AddProperty("Port", dataPort, null, (ushort)url.Port);
        builder.Editors.DataPort.AddProperty("MachineIdentifier", dataPort, null, machineIdentifier);
        builder.Editors.DataPort.AddProperty("ObjectDataQueueSize", dataPort, null, AnnaObjectDataQueueSize);
        builder.Editors.DataPort.AddProperty("RawDataQueueSize", dataPort, null, AnnaRawDataQueueSize);
    }

    private static void GenerateObjectData(Connection connection, ClusterBuilder builder, Dataflow dataflow, List<ProcessDataConfiguration> loggedProcessDataNodes,
                                    Dictionary<string, DataOutputInfo> dataOutputs,
                                    Dictionary<string, RotationalFrequencyOutputs> rotationalFrequencyOutputs,
                                    DataPortTreeNode objectDataTreeNode, ChildContainer cloudContainer, IDeviceTreeMasterNode deviceTreeMaster,
                                    Dictionary<string, AggregationFunctionCloudInputs> result)
    {
        var deviceContainerManager = new DeviceContainerManager(deviceTreeMaster, () => cloudContainer, dataflow, builder);

        foreach (var dataNode in loggedProcessDataNodes)
        {
            if (!dataOutputs.TryGetValue(dataNode.Node.Id, out var dataOutputInfo))
            {
                throw new InvalidOperationException($"No data output found for {dataNode.Node.Id}.");
            }

            var insertRotSpeedAndRefValue = rotationalFrequencyOutputs.TryGetValue(dataNode.Node.Id, out _);

            var annaObjectDataFb = AddAnnaObjectDataFb(builder, dataflow, deviceContainerManager, dataOutputInfo.DataPointIdentifiers[connection.Id], dataOutputInfo.Output.FunctionBlock.Name, dataNode.Configuration,
                    insertRotSpeedAndRefValue, insertRotSpeedAndRefValue, dataNode.Node);
            builder.Editors.DataPortTreeNode.AssignConnector(objectDataTreeNode, annaObjectDataFb.GetOutputByDesignId(FunctionBlocks.AnnaObjectData.Outputs.Value));

            result[dataNode.Node.Id] = new AggregationFunctionCloudInputs()
            {
                Avg = new CloudInput() { InputConnector = annaObjectDataFb.GetInputByDesignId(FunctionBlocks.AnnaObjectData.Inputs.Average) },
                Max = new CloudInput() { InputConnector = annaObjectDataFb.GetInputByDesignId(FunctionBlocks.AnnaObjectData.Inputs.Maximum) },
                Min = new CloudInput() { InputConnector = annaObjectDataFb.GetInputByDesignId(FunctionBlocks.AnnaObjectData.Inputs.Minimum) },
                RefValueAtMax = new CloudInput() { InputConnector = annaObjectDataFb.GetInputByDesignId(FunctionBlocks.AnnaObjectData.Inputs.RefValueAtMaximum) },
                RefValueAtMin = new CloudInput() { InputConnector = annaObjectDataFb.GetInputByDesignId(FunctionBlocks.AnnaObjectData.Inputs.RefValueAtMinimum) },
                RotSpeedAtMax = new CloudInput() { InputConnector = annaObjectDataFb.GetInputByDesignId(FunctionBlocks.AnnaObjectData.Inputs.RotSpeedAtMaximum) },
                RotSpeedAtMin = new CloudInput() { InputConnector = annaObjectDataFb.GetInputByDesignId(FunctionBlocks.AnnaObjectData.Inputs.RotSpeedAtMinimum) },
                Value = new CloudInput() { InputConnector = annaObjectDataFb.GetInputByDesignId(FunctionBlocks.AnnaObjectData.Inputs.Value) },
            };
        }
    }

    private static void GenerateRawData(Connection connection, ClusterBuilder builder, Dataflow dataflow, List<IDeviceTreeDataNode> loggedRawDataNodes, Container cloudContainer,
                         Dictionary<string, RotationalFrequencyOutputs> rotationalFrequencyOutputs,
                         DataPortTreeNode rawDataNode, Dictionary<string, AggregationFunctionCloudInputs> result)
    {
        var rawDataFbsByUnit = new Dictionary<string, FunctionBlock>();

        foreach (var dataNode in loggedRawDataNodes)
        {
            var unit = string.Empty;

            var rawDataFb = GetOrAddAnnaRawDataFb(builder, dataflow, unit, rawDataFbsByUnit, [.. rotationalFrequencyOutputs.Values.Select(r => r.RotationalFrequencyTuple)], connection.Name ?? "unknown", rawDataNode, cloudContainer);

            result[dataNode.Id] = new AggregationFunctionCloudInputs()
            {
                RawData = new CloudInput() { InputConnector = rawDataFb.GetInputByDesignId(FunctionBlocks.AnnaRawData.Inputs.Data) },
                RotationalFrequencies = new CloudInput() { InputConnector = rawDataFb.GetInputByDesignId(FunctionBlocks.AnnaRawData.Inputs.RotationalFrequencies) },
            };
        }
    }
}
