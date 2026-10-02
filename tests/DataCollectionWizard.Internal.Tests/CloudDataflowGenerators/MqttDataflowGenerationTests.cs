using System.Drawing;
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
/// Runs the full <see cref="DataflowGenerator"/> against an MQTT target, so the MQTT tree nodes are actually
/// wired to the device outputs and cluster-builder's type conversion rules are enforced.
/// </summary>
public class MqttDataflowGenerationTests
{
    private static readonly Guid s_subscriberDesignId = Guid.NewGuid();
    private static readonly Guid s_subscriberValueOutputId = Guid.NewGuid();
    private static readonly Guid s_subscriberAvailableOutputId = Guid.NewGuid();

    [Theory]
    [InlineData(DataType.Flag, typeof(double), "DataPointFloat")] // VSE / IO-Link flag subscribers log their numeric value (NV) output
    [InlineData(DataType.Flag, typeof(bool), "DataPointBool")]
    [InlineData(DataType.Whole, typeof(long), "DataPointInteger")]
    [InlineData(DataType.Whole, typeof(double), "DataPointFloat")] // IO-Link delivers Whole as double
    [InlineData(DataType.Real, typeof(double), "DataPointFloat")]
    [InlineData(DataType.Real, typeof(float), "DataPointFloat")]
    public void Connects_on_change_data_point_to_mqtt_node_matching_the_output_type(DataType dataType, Type outputType, string expectedDesignId)
    {
        // Arrange
        using var builder = CreateBuilder(outputType);
        var connection = new Connection { Id = Guid.NewGuid(), Name = "MyBroker", Type = ConnectionType.Mqtt };
        connection.SetMqttConnection(new MqttConnection { Address = "broker.example.com", Port = 1883 });

        var master = new DeviceTreeVseDevice { Alias = "Dev", Children = { CreateOnChangeNode(connection, dataType, "n1") }, Id = "dev", MacAddress = "aa:bb", Name = "Dev", Url = new Uri("http://10.0.0.1") };
        var generator = CreateGenerator(builder);
        var dataflow = builder.Editors.Cluster.AddDataflow("Dev", new Version(0, 1));
        var engine = AddEngine(builder);

        // Act
        generator.Generate(master, [connection], dataflow, engine, out _, out _, out _);

        // Assert
        var dataPort = Assert.Single(dataflow.DataPorts);
        var dataPointNode = Assert.Single(dataPort.TreeNodes.Single().Children.Single().Children); // edge -> device -> data point
        Assert.Equal(expectedDesignId, dataPointNode.DesignId);
        Assert.Equal(outputType == typeof(float) ? typeof(double) : outputType, dataPointNode.ValueType);
        Assert.Single(dataPointNode.IncomingLinks); // the subscriber output is assigned directly, without a compressor in between
    }

    [Theory]
    [InlineData(DataType.Text)]
    [InlineData(DataType.Octets)]
    [InlineData(DataType.Unknown)]
    public void Skips_enabled_nodes_whose_data_type_does_not_support_process_data_logging(DataType unsupportedDataType)
    {
        // Arrange
        using var builder = CreateBuilder(typeof(double));
        var connection = new Connection { Id = Guid.NewGuid(), Name = "MyBroker", Type = ConnectionType.Mqtt };
        connection.SetMqttConnection(new MqttConnection { Address = "broker.example.com", Port = 1883 });

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
        var dataPort = Assert.Single(dataflow.DataPorts);
        var dataPointNode = Assert.Single(dataPort.TreeNodes.Single().Children.Single().Children); // only the supported node is published
        Assert.Equal("supported", dataPointNode.Name);
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

    private static DataflowGenerator CreateGenerator(ClusterBuilder builder)
    {
        var instanceInfo = Substitute.For<IInstanceInformationProvider>();
        instanceInfo.Local.Name.Returns("Edge");

        return new DataflowGenerator(builder, Substitute.For<ILogger>(), "mid",
            [new FakeDeviceDataflowGenerator()], [new MqttCloudDataflowGenerator(instanceInfo)], [new MqttCloudFilter()]);
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
            ],
        };

        var resolver = Substitute.For<IDependencyResolver>();
        resolver.ResolveDataPortDesignDependency(Arg.Any<string>())
            .Returns(new ClusterDependency { Name = "Dataport", Version = "0.0.1" });
        resolver.ResolveFunctionBlockDesignDependency(Arg.Any<Guid>())
            .Returns(ci => new ClusterDependency { Name = ci.Arg<Guid>().ToString(), Version = "0.0.1" });
        resolver.ResolveFunctionBlockDesign(Arg.Any<Guid>())
            .Returns(ci => ci.Arg<Guid>() == s_subscriberDesignId ? subscriberDesign : new FunctionBlockDesign { Id = ci.Arg<Guid>(), Name = ci.Arg<Guid>().ToString() });

        var builder = new ClusterBuilder(resolver);
        builder.AddDataPortDesign(FunctionBlocks.MqttDataPort.DesignId);

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
    private sealed class FakeDeviceDataflowGenerator : IDeviceDataflowGenerator
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
                };
            }

            return result;
        }
    }
}
