using AwesomeAssertions;
using DataCollectionWizard.Backend.Consumers;
using DataCollectionWizard.Backend.DbContext;
using DataCollectionWizard.Backend.Extensions;
using DataCollectionWizard.Backend.Factories;
using DataCollectionWizard.Internal.Commands;
using DataCollectionWizard.Internal.Events;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Sdk.Testing.Backend;

namespace DataCollectionWizard.Backend.Tests.Consumers;

public class DeleteDeviceTreeConsumerTests : TestWithDbContextSqlite<DataCollectionWizardAttributeDbContextSqlite>
{
    private readonly Action<IBusRegistrationConfigurator> _configureServices;

    public DeleteDeviceTreeConsumerTests()
        => _configureServices = cfg =>
            {
                cfg.AddConsumer<DeleteDeviceTreeConsumer>();
                cfg.AddSingleton<IDataCollectionWizardDbContext>(_ => TestDbContext);
            };

    [Fact]
    public async Task Delete_existing_device_tree_should_trigger_change_event()
    {
        // Arrange
        await using var tester = new MassTransitTester(_configureServices);
        var deviceAddress = "address";
        var deviceTreeJson = FakeDeviceTreeFactory.CreateDeviceTree().SerializeToJson();
        var command = new DeleteDeviceTree(deviceAddress);

        await tester.Services.GetRequiredService<IDataCollectionWizardDbContext>()
            .SeedDeviceTree(deviceAddress, deviceTreeJson);

        // Act
        var changeEvent = await tester.TestCommand<DeleteDeviceTree, DeleteDeviceTreeConsumer, DeviceTreeChangedEvent>(command);

        // Assert
        changeEvent.Action.Should().Be(Sdk.Messaging.CrudAction.Deleted);
    }

    [Fact]
    public async Task Delete_not_existing_device_tree_should_do_nothing()
    {
        // Arrange
        await using var tester = new MassTransitTester(_configureServices);
        var deviceAddress = "address";
        var command = new DeleteDeviceTree(deviceAddress);

        // Act + Assert
        await tester.TestCommand<DeleteDeviceTree, DeleteDeviceTreeConsumer>(command);
    }
}
