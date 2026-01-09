using DataCollectionWizard.Backend.DbContext;
using Sdk.Testing.Backend;

namespace DataCollectionWizard.Backend.Tests.Consumers;

public class UpsertDeviceTreeConsumerTests : TestWithDbContextSqlite<DataCollectionWizardAttributeDbContextSqlite>
{
    //private readonly Action<IBusRegistrationConfigurator> _configureServices;

    //public UpsertDeviceTreeConsumerTests()
    //    => _configureServices = cfg =>
    //        {
    //            cfg.AddConsumer<ApplyDeviceTreeConsumer>();
    //            cfg.AddSingleton<IDataCollectionWizardDbContext>(_ => TestDbContext);
    //        };

    // TODO: Diese Tests funktionieren so nicht mehr seit der Consumer asynchron antwortet

    //[Fact]
    //public async Task Not_existing_device_tree_should_be_inserted()
    //{
    //    // Arrange
    //    await using var tester = new MassTransitTester(_configureServices);
    //    var deviceTree = FakeDeviceTreeFactory.CreateDeviceTree();
    //    var command = new ApplyDeviceTreeCommand(deviceTree, [], deviceTree.Children.OfType<IDeviceTreeMasterNode>().Select(m => m.Id));

    //    // Act
    //    var changeEvent = await tester.TestCommand<ApplyDeviceTreeCommand, ApplyDeviceTreeConsumer, DeviceTreeChangedEvent>(command);

    //    // Assert
    //    changeEvent.Action.Should().Be(Sdk.Messaging.CrudAction.Updated);
    //}

    //[Fact]
    //public async Task Existing_device_tree_should_be_updated()
    //{
    //    // Arrange
    //    await using var tester = new MassTransitTester(_configureServices);
    //    var deviceAddress = "address";
    //    var deviceTree = FakeDeviceTreeFactory.CreateDeviceTree();
    //    var command = new ApplyDeviceTreeCommand(deviceTree, [], deviceTree.Children.OfType<IDeviceTreeMasterNode>().Select(m => m.Id));

    //    tester.Services.GetRequiredService<IDataCollectionWizardDbContext>()
    //        .SeedDeviceTree(deviceAddress, JsonSerializer.Serialize(deviceTree.Children.First(), SerializerOptions.DeviceTree));

    //    // Act
    //    var changeEvent = await tester.TestCommand<ApplyDeviceTreeCommand, ApplyDeviceTreeConsumer, DeviceTreeChangedEvent>(command);

    //    // Assert
    //    changeEvent.Action.Should().Be(Sdk.Messaging.CrudAction.Updated);
    //}
}

