using ClusterManagement.Public.Connections.Contracts;
using ClusterManagement.Public.Connections.Extensions;
using DataCollectionWizard.Internal.Extensions;
using DataCollectionWizard.Internal.Services;
using DataCollectionWizard.Internal.Services.CloudDataflowGenerators;
using DataCollectionWizard.Internal.Services.DesignIds;
using DataCollectionWizard.Internal.Services.DeviceDataflowGenerators;
using DataCollectionWizard.Public;
using Microsoft.Extensions.Logging;
using Sdk.Connections.Contracts;
using ViciOne.Cluster.Builder;
using ViciOne.Cluster.Builder.Abstractions;
using ViciOne.Cluster.Model;
using FunctionBlockDesign = ViciOne.Core.Dataflow.DataModel.FunctionBlockDesign;
using ViciOne.DeviceTree.Contracts;

namespace DataCollectionWizard.Internal.Tests;

/// <summary>
/// Tests for DataflowGenerator.Generate. Blob recordings, event triggers and raw data settings are only
/// passed on for clouds whose cloud filter supports configuring them. Settings left enabled for any other
/// cloud (MQTT, OPC UA) are ignored instead of failing the whole generation.
/// </summary>
public class DataflowGeneratorTests
{
    private const string RawDataNodeId = "vse@127.0.0.1//RawData/Sensor 1";
    private const string ProcessDataNodeId = "vse@127.0.0.1//Value";

    [Fact]
    public void Ignores_blob_settings_enabled_for_a_cloud_that_cannot_publish_them()
    {
        // Arrange
        var opcUaConnection = CreateOpcUaConnection();
        var master = CreateMaster(opcUaConnection.Id);
        var deviceGenerator = new FakeDeviceDataflowGenerator();
        using var builder = CreateBuilder(out var dataflow, out var engine);
        var generator = CreateDataflowGenerator(builder, deviceGenerator,
            [new OpcUaCloudDataflowGenerator()], [new OpcUaCloudFilter()]);

        // Act
        var exception = Record.Exception(() => generator.Generate(master, [opcUaConnection], dataflow, engine, out _, out _, out _));

        // Assert
        Assert.Null(exception);
        Assert.Empty(deviceGenerator.BlobLoggingConfigurations!);
        Assert.False(deviceGenerator.EnabledDataIds![RawDataNodeId]); // no subscription for raw data nobody publishes
        Assert.True(deviceGenerator.EnabledDataIds[ProcessDataNodeId]); // process values still reach OPC UA
    }

    [Fact]
    public void Passes_blob_settings_on_for_a_cloud_that_supports_them()
    {
        // Arrange
        var annaConnection = new Connection { Id = Guid.NewGuid(), Name = "Anna", Tags = [Constants.AnnaCloud], Type = ConnectionType.Http };
        var master = CreateMaster(annaConnection.Id);
        var deviceGenerator = new FakeDeviceDataflowGenerator();
        using var builder = CreateBuilder(out var dataflow, out var engine);

        // Only the filter is needed: it decides which configurations are passed on to the device generator.
        var generator = CreateDataflowGenerator(builder, deviceGenerator, [], [new AnnaCloudFilter()]);

        // Act
        generator.Generate(master, [annaConnection], dataflow, engine, out _, out _, out _);

        // Assert
        Assert.Contains(deviceGenerator.BlobLoggingConfigurations!, c => c.NeedsScheduler && c.DataGroupIdentifier == annaConnection.Id);
        Assert.Contains(deviceGenerator.BlobLoggingConfigurations!, c => c.NeedsEventTrigger && c.DataGroupIdentifier == annaConnection.Id);
        Assert.True(deviceGenerator.EnabledDataIds![RawDataNodeId]);
    }

    private static Connection CreateOpcUaConnection()
    {
        var connection = new Connection { Id = Guid.NewGuid(), Name = "OPC/UA", Type = ConnectionType.OpcUaServer };
        connection.SetOpcUaServerConnection(new OpcUaServerConnection { Port = 4840, Server = "0.0.0.0" });
        return connection;
    }

    // A VSE with one process value and one raw data sensor, where the sensor has a blob recording, an event
    // trigger and raw data settings enabled for the given connection.
    private static DeviceTreeVseDevice CreateMaster(Guid connectionId)
    {
        var processData = new DeviceTreeProcessData { DataType = DataType.Real, Id = ProcessDataNodeId, Name = "Value" };
        processData.CompressorConfigurations.Add(new CompressorConfiguration { DataGroupIdentifier = connectionId, Enabled = true });

        var rawData = new DeviceTreeVseRawData
        {
            Id = RawDataNodeId,
            Index = 1,
            Name = "Sensor 1",
            RawDataConfigurations = { { connectionId, new RawDataSettings { Duration = 4000, Frequency = 100000 } } },
            SchedulerConfigurations =
            {
                new()
                {
                    DataGroupIdentifier = connectionId,
                    Enabled = true,
                    Times = { { DayOfWeek.Monday, [TimeSpan.FromSeconds(0)] } },
                },
            },
        };
        rawData.EventTriggerConfigurations.Add(new()
        {
            IsSensorConfigured = true,
            Name = "Limit",
            ReferenceNodeId = "vse@127.0.0.1//Objects/Object01__!__Limit",
            Triggers = { new() { DataGroupIdentifier = connectionId, Delay = 1, Enabled = true, OnDamage = true } },
        });

        return new DeviceTreeVseDevice
        {
            Alias = "Dev",
            Children = [processData, rawData],
            Id = "vse@127.0.0.1",
            MacAddress = "aa:bb",
            Name = "Dev",
            Url = new Uri("http://127.0.0.1"),
        };
    }

    private static ClusterBuilder CreateBuilder(out Dataflow dataflow, out Engine engine)
    {
        var resolver = Substitute.For<IDependencyResolver>();
        resolver.ResolveDataPortDesignDependency(Arg.Any<string>())
            .Returns(new ClusterDependency { Name = "Dataport", Version = "0.0.1" });
        resolver.ResolveFunctionBlockDesignDependency(Arg.Any<Guid>())
            .Returns(call => new ClusterDependency { Name = call.Arg<Guid>().ToString(), Version = "0.0.1" });
        resolver.ResolveFunctionBlockDesign(Arg.Any<Guid>())
            .Returns(call => new FunctionBlockDesign { Id = call.Arg<Guid>(), Name = call.Arg<Guid>().ToString() });

        var builder = new ClusterBuilder(resolver);
        builder.AddDataPortDesign(FunctionBlocks.OpcUaDataPort.DesignId);
        dataflow = builder.Editors.Cluster.AddDataflow("Test", new Version(0, 1));

        var nodeGroup = builder.Editors.Cluster.AddNodeGroup();
        var node = builder.Editors.NodeGroup.AddNode(nodeGroup);
        var application = builder.Editors.Node.AddApplication(node, ClusterApplicationType.CoreOsStandalone);
        var engineHost = builder.Editors.Application.AddEngineHost(application, "Host");
        engine = builder.Editors.EngineHost.AddEngine(engineHost, "Engine");

        return builder;
    }

    private static DataflowGenerator CreateDataflowGenerator(ClusterBuilder builder, FakeDeviceDataflowGenerator deviceGenerator,
        List<ICloudDataflowGenerator> cloudGenerators, List<ICloudFilter> cloudFilters)
        => new(builder, Substitute.For<ILogger>(), "mid", [deviceGenerator], cloudGenerators, cloudFilters);

    /// <summary>
    /// Records what the DataflowGenerator asks the device to generate. It returns no raw data, so no blob
    /// function blocks are wired up, and the test only checks which configurations were passed on.
    /// </summary>
    private sealed class FakeDeviceDataflowGenerator : IDeviceDataflowGenerator
    {
        public Type DeviceType => typeof(DeviceTreeVseDevice);

        public BlobLoggingConfiguration[]? BlobLoggingConfigurations { get; private set; }

        public Dictionary<string, bool>? EnabledDataIds { get; private set; }

        public DeviceDataflowGeneratorResult GenerateDeviceFunctionBlocks(ClusterBuilder builder, Dataflow dataflow, IDeviceTreeMasterNode device, Dictionary<string, bool> enabledDataIds,
            Dictionary<Guid, string> cloudNames, BlobLoggingConfiguration[] blobLoggingConfigurations, string connectionIdentifier)
        {
            EnabledDataIds = enabledDataIds;
            BlobLoggingConfigurations = blobLoggingConfigurations;
            return new DeviceDataflowGeneratorResult();
        }

        public DeviceTreeFunctionBlockResult GenerateDeviceTreeSourceFunctionBlock(ClusterBuilder builder, Dataflow dataflow, IDeviceTreeMasterNode master, string connectionIdentifier)
            => new();
    }
}
