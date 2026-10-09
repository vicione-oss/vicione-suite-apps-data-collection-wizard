using System.Drawing;
using ClusterManagement.Public.Connections.Contracts;
using ClusterManagement.Public.Connections.Extensions;
using DataCollectionWizard.Internal.Contracts;
using DataCollectionWizard.Internal.Extensions;
using DataCollectionWizard.Internal.Services;
using DataCollectionWizard.Internal.Services.CloudDataflowGenerators;
using DataCollectionWizard.Internal.Services.DesignIds;
using DataCollectionWizard.Internal.Services.DeviceDataflowGenerators;
using Microsoft.Extensions.Logging;
using Sdk.Connections.Contracts;
using Sdk.Connections.Extensions;
using Sdk.Instance;
using ViciOne.Cluster.Builder;
using ViciOne.Cluster.Builder.Abstractions;
using ViciOne.Cluster.Model;
using ConnectorDesign = ViciOne.Core.Dataflow.DataModel.ConnectorDesign;
using FunctionBlockDesign = ViciOne.Core.Dataflow.DataModel.FunctionBlockDesign;
using ViciOne.DeviceTree.Contracts;

namespace DataCollectionWizard.Internal.Tests.CloudDataflowGenerators;

/// <summary>
/// Runs the full <see cref="DataflowGenerator"/> against the tree based targets (MQTT and OPC UA), so their tree
/// nodes are actually wired to the device outputs and cluster-builder's type conversion rules are enforced.
/// </summary>
public class TreeDataflowGenerationTests
{
    public enum Target
    {
        Mqtt,
        OpcUa,
    }

    private static readonly Guid s_subscriberDesignId = Guid.NewGuid();
    private static readonly Guid s_subscriberValueOutputId = Guid.NewGuid();
    private static readonly Guid s_subscriberAvailableOutputId = Guid.NewGuid();
    private static readonly Guid s_subscriberUnitOutputId = Guid.NewGuid();

    public static TheoryData<Target, DataType, Type, string> OnChangeCases()
    {
        var data = new TheoryData<Target, DataType, Type, string>();

        foreach (var target in Enum.GetValues<Target>())
        {
            data.Add(target, DataType.Flag, typeof(double), "DataPointFloat"); // VSE / IO-Link flag subscribers log their numeric value (NV) output
            data.Add(target, DataType.Flag, typeof(bool), "DataPointBool");
            data.Add(target, DataType.Whole, typeof(long), "DataPointInteger");
            data.Add(target, DataType.Whole, typeof(double), "DataPointFloat"); // IO-Link delivers Whole as double
            data.Add(target, DataType.Real, typeof(double), "DataPointFloat");
            data.Add(target, DataType.Real, typeof(float), "DataPointFloat");
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(OnChangeCases))]
    public void Connects_on_change_data_point_to_node_matching_the_output_type(Target target, DataType dataType, Type outputType, string expectedDesignId)
    {
        // Arrange
        using var builder = CreateBuilder(outputType);
        var connection = CreateConnection(target);
        var master = new DeviceTreeVseDevice { Alias = "Dev", Children = { CreateOnChangeNode(connection, dataType, "n1") }, Id = "dev", MacAddress = "aa:bb", Name = "Dev", Url = new Uri("http://10.0.0.1") };
        var generator = CreateGenerator(builder);
        var dataflow = builder.Editors.Cluster.AddDataflow("Dev", new Version(0, 1));
        var engine = AddEngine(builder);

        // Act
        generator.Generate(master, [connection], dataflow, engine, out _, out _, out _);

        // Assert
        var dataPointNode = Assert.Single(GetDataPointNodes(Assert.Single(dataflow.DataPorts)));
        Assert.Equal(expectedDesignId, dataPointNode.DesignId);
        Assert.Equal(outputType == typeof(float) ? typeof(double) : outputType, dataPointNode.ValueType);
        Assert.Single(dataPointNode.IncomingLinks); // the subscriber output is assigned directly, without a compressor in between
    }

    [Theory]
    [InlineData(Target.Mqtt, DataType.Text)]
    [InlineData(Target.Mqtt, DataType.Octets)]
    [InlineData(Target.Mqtt, DataType.Unknown)]
    [InlineData(Target.OpcUa, DataType.Text)]
    [InlineData(Target.OpcUa, DataType.Octets)]
    [InlineData(Target.OpcUa, DataType.Unknown)]
    public void Skips_enabled_nodes_whose_data_type_does_not_support_process_data_logging(Target target, DataType unsupportedDataType)
    {
        // Arrange
        using var builder = CreateBuilder(typeof(double));
        var connection = CreateConnection(target);
        var master = new DeviceTreeVseDevice
        {
            Alias = "Dev",
            Children = { CreateOnChangeNode(connection, DataType.Real, "supported"), CreateOnChangeNode(connection, unsupportedDataType, "unsupported") },
            Id = "dev",
            MacAddress = "aa:bb",
            Name = "Dev",
            Url = new Uri("http://10.0.0.1"),
        };
        var generator = CreateGenerator(builder);
        var dataflow = builder.Editors.Cluster.AddDataflow("Dev", new Version(0, 1));
        var engine = AddEngine(builder);

        // Act
        generator.Generate(master, [connection], dataflow, engine, out _, out _, out _);

        // Assert
        var dataPointNode = Assert.Single(GetDataPointNodes(Assert.Single(dataflow.DataPorts))); // only the supported node is published
        Assert.Equal("supported", dataPointNode.Name);
    }

    [Fact]
    public void Puts_timestamp_and_unit_on_the_message_of_an_mqtt_5_data_point()
    {
        // Arrange
        using var builder = CreateBuilder(typeof(double));
        var connection = CreateMqttConnection(MqttProtocolVersion.V500);
        var master = new DeviceTreeVseDevice { Alias = "Dev", Children = { CreateOnChangeNode(connection, DataType.Real, "n1") }, Id = "dev", MacAddress = "aa:bb", Name = "Dev", Url = new Uri("http://10.0.0.1") };
        var generator = CreateGenerator(builder);
        var dataflow = builder.Editors.Cluster.AddDataflow("Dev", new Version(0, 1));
        var engine = AddEngine(builder);

        // Act
        generator.Generate(master, [connection], dataflow, engine, out _, out _, out _);

        // Assert
        var dataPointNode = Assert.Single(GetDataPointNodes(Assert.Single(dataflow.DataPorts)));
        Assert.Collection(dataPointNode.Children,
            timestamp =>
            {
                Assert.Equal("Timestamp", timestamp.DesignId);
                Assert.Equal(typeof(DateTime), timestamp.ValueType);
                Assert.Equal(DataPortTransferMode.None, timestamp.TransferMode);
                Assert.Empty(timestamp.IncomingLinks); // the DataPort fills in the timestamp of the published value itself
            },
            unit =>
            {
                Assert.Equal("UserProperty", unit.DesignId);
                Assert.Equal(MqttCloudDataflowGenerator.UnitUserPropertyKey, unit.Name);
                Assert.Equal(typeof(string), unit.ValueType);
                Assert.Equal(DataPortTransferMode.None, unit.TransferMode);
                Assert.Single(unit.IncomingLinks);
            });
    }

    [Fact]
    public void Puts_only_the_timestamp_on_the_message_when_the_device_reports_no_unit()
    {
        // Arrange
        using var builder = CreateBuilder(typeof(double));
        var connection = CreateMqttConnection(MqttProtocolVersion.V500);
        var master = new DeviceTreeVseDevice { Alias = "Dev", Children = { CreateOnChangeNode(connection, DataType.Real, "n1") }, Id = "dev", MacAddress = "aa:bb", Name = "Dev", Url = new Uri("http://10.0.0.1") };
        var generator = CreateGenerator(builder, reportsUnit: false);
        var dataflow = builder.Editors.Cluster.AddDataflow("Dev", new Version(0, 1));
        var engine = AddEngine(builder);

        // Act
        generator.Generate(master, [connection], dataflow, engine, out _, out _, out _);

        // Assert
        var dataPointNode = Assert.Single(GetDataPointNodes(Assert.Single(dataflow.DataPorts)));
        Assert.Equal("Timestamp", Assert.Single(dataPointNode.Children).DesignId);
    }

    [Fact]
    public void Adds_no_envelope_children_to_an_mqtt_3_1_1_data_point()
    {
        // Arrange
        using var builder = CreateBuilder(typeof(double));
        var connection = CreateMqttConnection(MqttProtocolVersion.V311);
        var master = new DeviceTreeVseDevice { Alias = "Dev", Children = { CreateOnChangeNode(connection, DataType.Real, "n1") }, Id = "dev", MacAddress = "aa:bb", Name = "Dev", Url = new Uri("http://10.0.0.1") };
        var generator = CreateGenerator(builder);
        var dataflow = builder.Editors.Cluster.AddDataflow("Dev", new Version(0, 1));
        var engine = AddEngine(builder);

        // Act
        generator.Generate(master, [connection], dataflow, engine, out _, out _, out _);

        // Assert
        var dataPointNode = Assert.Single(GetDataPointNodes(Assert.Single(dataflow.DataPorts)));
        Assert.Empty(dataPointNode.Children); // the DataPort refuses to start with envelope children on MQTT 3.1.1
    }

    [Fact]
    public void Adds_no_envelope_children_to_an_opc_ua_data_point()
    {
        // Arrange
        using var builder = CreateBuilder(typeof(double));
        var connection = CreateConnection(Target.OpcUa);
        var master = new DeviceTreeVseDevice { Alias = "Dev", Children = { CreateOnChangeNode(connection, DataType.Real, "n1") }, Id = "dev", MacAddress = "aa:bb", Name = "Dev", Url = new Uri("http://10.0.0.1") };
        var generator = CreateGenerator(builder);
        var dataflow = builder.Editors.Cluster.AddDataflow("Dev", new Version(0, 1));
        var engine = AddEngine(builder);

        // Act
        generator.Generate(master, [connection], dataflow, engine, out _, out _, out _);

        // Assert
        var dataPointNode = Assert.Single(GetDataPointNodes(Assert.Single(dataflow.DataPorts)));
        Assert.Empty(dataPointNode.Children);
    }

    private static Connection CreateMqttConnection(MqttProtocolVersion protocolVersion)
    {
        var connection = new Connection { Id = Guid.NewGuid(), Name = "MyBroker", Type = ConnectionType.Mqtt };
        connection.SetMqttConnection(new MqttConnection { Address = "broker.example.com", Port = 1883, ProtocolVersion = protocolVersion });
        return connection;
    }

    private static Connection CreateConnection(Target target)
    {
        switch (target)
        {
            case Target.Mqtt:
                var mqttConnection = new Connection { Id = Guid.NewGuid(), Name = "MyBroker", Type = ConnectionType.Mqtt };
                mqttConnection.SetMqttConnection(new MqttConnection { Address = "broker.example.com", Port = 1883 });
                return mqttConnection;

            case Target.OpcUa:
                var opcUaConnection = new Connection { Id = Guid.NewGuid(), Name = "MyServer", Type = ConnectionType.OpcUaServer };
                // The local interface needs no host lookup, so the system configuration service stays unused.
                opcUaConnection.SetOpcUaServerConnection(new OpcUaServerConnection { ApplicationCertificatesPath = "own", NetworkInterface = OpcUaServerConnection.LocalNetworkInterface, Port = 4840 });
                return opcUaConnection;

            default:
                throw new ArgumentOutOfRangeException(nameof(target));
        }
    }

    // The data points are the nodes below the target specific folder scaffold (edge/device for MQTT, device for OPC UA),
    // without the envelope children an MQTT data point carries.
    private static IEnumerable<DataPortTreeNode> GetDataPointNodes(DataPort dataPort)
    {
        var pending = new Stack<DataPortTreeNode>(dataPort.TreeNodes);

        while (pending.TryPop(out var node))
        {
            if (node.DesignId.StartsWith("DataPoint", StringComparison.Ordinal))
                yield return node;

            foreach (var child in node.Children)
                pending.Push(child);
        }
    }

    private static DeviceTreeProcessData CreateOnChangeNode(Connection connection, DataType dataType, string id)
        => new()
        {
            CompressorConfigurations =
            {
                new CompressorConfiguration
                {
                    Aggregation = AggregationFunction.Last,
                    CompressionTime = (int)AggregationInterval.OnChange,
                    DataGroupIdentifier = connection.Id,
                    Enabled = true,
                },
            },
            DataType = dataType,
            Id = id,
            Name = id,
        };

    private static DataflowGenerator CreateGenerator(ClusterBuilder builder, bool reportsUnit = true)
    {
        var instanceInfo = Substitute.For<IInstanceInformationProvider>();
        instanceInfo.Local.Name.Returns("Edge");

        return new DataflowGenerator(builder, Substitute.For<ILogger>(), "mid", [], [new FakeDeviceDataflowGenerator(reportsUnit)],
            [new MqttCloudDataflowGenerator(instanceInfo), new OpcUaCloudDataflowGenerator()],
            [new MqttCloudFilter(), new OpcUaCloudFilter()]);
    }

    private static ClusterBuilder CreateBuilder(Type outputType)
    {
        var subscriberDesign = new FunctionBlockDesign
        {
            Id = s_subscriberDesignId,
            Name = "FakeSubscriber",
            ProcessDataOutputs =
            [
                CreateOutputDesign(outputType, s_subscriberValueOutputId, "Value"),
                new ViciOne.Core.Dataflow.DataModel.ConnectorDesignOutput<bool> { Id = s_subscriberAvailableOutputId, Name = "Available" },
                new ViciOne.Core.Dataflow.DataModel.ConnectorDesignOutput<string> { Id = s_subscriberUnitOutputId, Name = "Unit" },
            ],
        };

        var resolver = Substitute.For<IDependencyResolver>();
        resolver.ResolveDataPortDesignDependency(Arg.Any<string>())
            .Returns(ci => new ClusterDependency { Name = ci.Arg<string>(), Version = "0.0.1" });
        resolver.ResolveFunctionBlockDesignDependency(Arg.Any<Guid>())
            .Returns(ci => new ClusterDependency { Name = ci.Arg<Guid>().ToString(), Version = "0.0.1" });
        resolver.ResolveFunctionBlockDesign(Arg.Any<Guid>())
            .Returns(ci => ci.Arg<Guid>() == s_subscriberDesignId ? subscriberDesign : new FunctionBlockDesign { Id = ci.Arg<Guid>(), Name = ci.Arg<Guid>().ToString() });

        var builder = new ClusterBuilder(resolver);
        builder.AddDataPortDesign(FunctionBlocks.MqttDataPort.DesignId);
        builder.AddDataPortDesign(FunctionBlocks.OpcUaDataPort.DesignId);

        return builder;
    }

    private static ConnectorDesign CreateOutputDesign(Type type, Guid id, string name)
    {
        var design = (ConnectorDesign)Activator.CreateInstance(typeof(ViciOne.Core.Dataflow.DataModel.ConnectorDesignOutput<>).MakeGenericType(type))!;
        design.Id = id;
        design.Name = name;
        return design;
    }

    private static Engine AddEngine(ClusterBuilder builder)
    {
        var nodeGroup = builder.Editors.Cluster.AddNodeGroup();
        var node = builder.Editors.NodeGroup.AddNode(nodeGroup);
        var application = builder.Editors.Node.AddApplication(node, ClusterApplicationType.CoreOsStandalone);
        var engineHost = builder.Editors.Application.AddEngineHost(application, "Host");

        return builder.Editors.EngineHost.AddEngine(engineHost, "Dev");
    }

    /// <summary>
    /// Creates one subscriber function block per data node, whose value output has the type given by the builder's
    /// subscriber design. Stands in for the VSE / IO-Link generators, which need a live device.
    /// </summary>
    private sealed class FakeDeviceDataflowGenerator(bool reportsUnit) : IDeviceDataflowGenerator
    {
        public Type DeviceType => typeof(DeviceTreeVseDevice);

        public DeviceTreeFunctionBlockResult GenerateDeviceTreeSourceFunctionBlock(ClusterBuilder builder, Dataflow dataflow, IDeviceTreeMasterNode master, string connectionIdentifier)
            => new();

        public DeviceDataflowGeneratorResult GenerateDeviceFunctionBlocks(ClusterBuilder builder, Dataflow dataflow, IDeviceTreeMasterNode device, Dictionary<string, bool> enabledDataIds,
            Dictionary<Guid, string> cloudNames, BlobLoggingConfiguration[] blobLoggingConfigurations, string connectionIdentifier)
        {
            var result = new DeviceDataflowGeneratorResult();

            foreach (var id in enabledDataIds.Where(e => e.Value).Select(e => e.Key))
            {
                var subscriber = builder.Editors.Container.AddFunctionBlock(dataflow, s_subscriberDesignId, id, null, new Point());

                result.DataOutputs[id] = new DataOutputInfo
                {
                    AvailableOutput = subscriber.GetOutputByDesignId(s_subscriberAvailableOutputId),
                    Output = subscriber.GetOutputByDesignId(s_subscriberValueOutputId),
                    Suffix = id,
                    UnitOutput = reportsUnit ? subscriber.GetOutputByDesignId(s_subscriberUnitOutputId) : null,
                };
            }

            return result;
        }
    }
}
