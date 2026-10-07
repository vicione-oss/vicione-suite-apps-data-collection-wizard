using DataCollectionWizard.Internal.Services.DesignIds;
using DataCollectionWizard.Internal.Services.DeviceDataflowGenerators;
using Microsoft.Extensions.Logging;
using ViciOne.Cluster.Builder;
using ViciOne.Cluster.Builder.Abstractions;
using ViciOne.Cluster.Model;
using ViciOne.DeviceTree.Contracts;
using FunctionBlockDesign = ViciOne.Core.Dataflow.DataModel.FunctionBlockDesign;

namespace DataCollectionWizard.Internal.Tests.DeviceDataflowGenerators;

/// <summary>
/// Tests for VseDataflowGenerator.GenerateDeviceTreeSourceFunctionBlock, which adds the subscriber that reads a VSE's
/// device tree. The subscriber's name carries the same "host:port" address its Url setting is set to.
/// </summary>
public class VseDataflowGeneratorTests
{
    [Theory]
    [InlineData("http://127.0.0.1:3321", "127.0.0.1:3321")]
    [InlineData("http://127.0.0.1", "127.0.0.1:3321")]
    [InlineData("http://localhost:3321", "localhost:3321")]
    [InlineData("http://vse-sim:4000", "vse-sim:4000")]
    public void GenerateDeviceTreeSourceFunctionBlock_KeepsHostAndPort(string url, string expectedAddress)
    {
        // Arrange
        using var builder = CreateBuilder(out var dataflow);
        var master = new DeviceTreeVseDevice { Alias = "Dev", Id = "id", MacAddress = "aa:bb", Name = "Dev", Url = new Uri(url) };
        var generator = new VseDataflowGenerator(Substitute.For<ILogger<VseDataflowGenerator>>());

        // Act
        generator.GenerateDeviceTreeSourceFunctionBlock(builder, dataflow, master, "connection");

        // Assert
        var subscriber = Assert.Single(dataflow.Root.FunctionBlocks, f => f.DesignId == FunctionBlocks.VseDeviceTreeSubscriber.DesignId);
        Assert.Equal($"TreeSubscriber {expectedAddress}", subscriber.Name);
        var urlSetting = Assert.Single(subscriber.Settings, s => s.DesignId == FunctionBlocks.VseDeviceTreeSubscriber.Settings.Url);
        Assert.Equal(expectedAddress, urlSetting.Value);
    }

    private static ClusterBuilder CreateBuilder(out Dataflow dataflow)
    {
        var subscriberDesign = new FunctionBlockDesign
        {
            Id = FunctionBlocks.VseDeviceTreeSubscriber.DesignId,
            Name = "VseDeviceTreeSubscriber",
            ProcessDataInputs = [new ViciOne.Core.Dataflow.DataModel.ConnectorDesignInput<bool> { Id = FunctionBlocks.VseDeviceTreeSubscriber.Inputs.Trigger, Name = "Trigger" }],
            ProcessDataOutputs = [new ViciOne.Core.Dataflow.DataModel.ConnectorDesignOutput<string> { Id = FunctionBlocks.VseDeviceTreeSubscriber.Outputs.DeviceTree, Name = "DeviceTree" }],
            Settings = [new ViciOne.Core.Dataflow.DataModel.SettingDesign<string> { Id = FunctionBlocks.VseDeviceTreeSubscriber.Settings.Url, Name = "Url" }],
        };

        var resolver = Substitute.For<IDependencyResolver>();
        resolver.ResolveFunctionBlockDesignDependency(Arg.Any<Guid>())
            .Returns(call => new ClusterDependency { Name = call.Arg<Guid>().ToString(), Version = "0.0.1" });
        resolver.ResolveFunctionBlockDesign(Arg.Any<Guid>())
            .Returns(call => call.Arg<Guid>() == subscriberDesign.Id
                ? subscriberDesign
                : new FunctionBlockDesign { Id = call.Arg<Guid>(), Name = call.Arg<Guid>().ToString() });

        var builder = new ClusterBuilder(resolver);
        dataflow = builder.Editors.Cluster.AddDataflow("Test", new Version(0, 1));

        return builder;
    }
}
