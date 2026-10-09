using DataCollectionWizard.Internal.Services.DesignIds;
using DataCollectionWizard.Internal.Services.DeviceDataflowGenerators;
using Sdk.Connections.Contracts;
using Sdk.Connections.Extensions;
using Sdk.Instance;
using Sdk.SystemConfiguration.Contracts;
using ViciOne.Cluster.Builder;
using ViciOne.Cluster.Model;
using ViciOne.DeviceTree.Contracts;

namespace DataCollectionWizard.Internal.Services.CloudDataflowGenerators;

public sealed class MqttCloudDataflowGenerator(IInstanceInformationProvider instanceInformationProvider) : CloudDataflowTreeGenerator, ICloudDataflowGenerator
{
    // Envelope child design ids of the MQTT DataPort (Mqtt.yaml), which put a value on the message of their parent data point.
    private const string PortDesignIdTimestamp = "Timestamp";
    private const string PortDesignIdUserProperty = "UserProperty";

    internal const string TimestampNodeName = "Timestamp";
    internal const string UnitUserPropertyKey = "unit";

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
                                                                            List<IDeviceTreeDataNode> loggedRawDataNodes,
                                                                            IReadOnlyList<NetworkInterface> hostNetworkInterfaces)
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

        // MQTT 3.1.1 has no user properties, and the DataPort refuses to start with envelope children on it.
        Action<DataPortTreeNode, DataOutputInfo?>? addEnvelopeChildren = connection.GetMqttConnection()!.ProtocolVersion == MqttProtocolVersion.V500
            ? (dataPointNode, dataOutput) => AddEnvelopeChildren(builder, dataPointNode, dataOutput)
            : null;

        BuildDataportNodesRecursively(loggedTree.Children, dataport, deviceNode, builder, dataOutputs, result, addEnvelopeChildren);

        return result;
    }

    private protected override string GetSafeNodeName(string name)
        // Replace any characters that are not allowed in MQTT topic names with underscores
        => new([.. name.Select(c => char.IsLetterOrDigit(c) || c == '-' || c == '_' ? c : '_')]);

    /// <summary>
    /// Puts the timestamp of each published value and, if the device reports one, its unit on the message as user properties.
    /// The DataPort fills the timestamp from the value itself; the unit is fed from the device's unit output.
    /// </summary>
    internal static void AddEnvelopeChildren(ClusterBuilder builder, DataPortTreeNode dataPointNode, DataOutputInfo? dataOutput)
    {
        builder.Editors.DataPortTreeNode.AddTreeNode(PortDesignIdTimestamp, dataPointNode, TimestampNodeName, typeof(DateTime), DataPortTransferMode.None);

        if (dataOutput?.UnitOutput is not { } unitOutput)
            return;

        var unitNode = builder.Editors.DataPortTreeNode.AddTreeNode(PortDesignIdUserProperty, dataPointNode, UnitUserPropertyKey, typeof(string), DataPortTransferMode.None);
        builder.Editors.DataPortTreeNode.AssignConnector(unitNode, unitOutput);
    }

    private static DataPort GenerateDataPort(Connection connection, IDeviceTreeMasterNode deviceTreeMaster, ClusterBuilder builder, Dataflow dataflow)
    {
        var dataPort = builder.Editors.Dataflow.AddDataPort(dataflow, FunctionBlocks.MqttDataPort.DesignId,
                                    $"{connection.Name} - {deviceTreeMaster.Url}", DataPortDirection.Out, FunctionBlocks.MqttDataPort.Type);

        MqttDataPortProperties.Add(builder, dataPort, connection.GetMqttConnection()!);

        return dataPort;
    }
}
