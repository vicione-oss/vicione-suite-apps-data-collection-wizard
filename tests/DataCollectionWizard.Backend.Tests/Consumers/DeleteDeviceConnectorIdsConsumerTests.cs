using AwesomeAssertions;
using DataCollectionWizard.Backend.Consumers;
using DataCollectionWizard.Backend.DbContext;
using DataCollectionWizard.Internal.Commands;
using DataCollectionWizard.Internal.Contracts;
using DataCollectionWizard.Internal.Events;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Sdk.Testing.Backend;

namespace DataCollectionWizard.Backend.Tests.Consumers;

public class DeleteDeviceConnectorIdsConsumerTests : TestWithDbContextSqlite<DataCollectionWizardAttributeDbContextSqlite>
{
    private readonly Action<IBusRegistrationConfigurator> _configureServices;
    private readonly DeviceConnectorIds _device1ConnectorIds1 = new()
    {
        DeviceAddress = "Address1",
        DeviceTreeOutput = Guid.NewGuid(),
        TriggerInput = Guid.NewGuid(),
    };

    private readonly DeviceConnectorIds _device1ConnectorIds2 = new()
    {
        DeviceAddress = "Address1",
        DeviceTreeOutput = Guid.NewGuid(),
        TriggerInput = Guid.NewGuid(),
    };

    private readonly DeviceConnectorIds _device2ConnectorIds1 = new()
    {
        DeviceAddress = "Address1",
        DeviceTreeOutput = Guid.NewGuid(),
        TriggerInput = Guid.NewGuid(),
    };

    public DeleteDeviceConnectorIdsConsumerTests()
        => _configureServices = cfg =>
            {
                cfg.AddConsumer<DeleteDeviceConnectorIdsConsumer>();
                cfg.AddSingleton<IDataCollectionWizardDbContext>(_ => TestDbContext);
            };

    [Fact]
    public async Task Delete_existing_ids_should_be_trigger_change_event()
    {
        // Arrange
        await using var tester = new MassTransitTester(_configureServices);
        var command = new DeleteDeviceConnectorIds([_device1ConnectorIds1, _device1ConnectorIds2, _device2ConnectorIds1]);

        tester.Services.GetRequiredService<IDataCollectionWizardDbContext>()
            .SeedDeviceConnectorIds(_device1ConnectorIds1, _device1ConnectorIds2, _device2ConnectorIds1);

        // Act
        var changeEvent = await tester.TestCommand<DeleteDeviceConnectorIds, DeleteDeviceConnectorIdsConsumer, DeviceConnectorIdsChangedEvent>(command);

        // Assert
        changeEvent.ChangedItems.Should().HaveCount(3);
        changeEvent.ChangedItems.Should().AllSatisfy(k => k.Action.Should().Be(Sdk.Messaging.CrudAction.Deleted));
    }

    [Fact]
    public async Task Delete_not_existing_ids_should_be_trigger_empty_change_event()
    {
        // Arrange
        await using var tester = new MassTransitTester(_configureServices);
        var command = new DeleteDeviceConnectorIds([_device1ConnectorIds1, _device1ConnectorIds2, _device2ConnectorIds1]);

        // Act
        var changeEvent = await tester.TestCommand<DeleteDeviceConnectorIds, DeleteDeviceConnectorIdsConsumer, DeviceConnectorIdsChangedEvent>(command);

        // Assert
        changeEvent.ChangedItems.Should().BeEmpty();
    }

    [Fact]
    public async Task Only_deleted_ids_should_be_contained_in_change_event()
    {
        // Arrange
        await using var tester = new MassTransitTester(_configureServices);
        var command = new DeleteDeviceConnectorIds([_device1ConnectorIds1, _device1ConnectorIds2, _device2ConnectorIds1]);

        tester.Services.GetRequiredService<IDataCollectionWizardDbContext>()
            .SeedDeviceConnectorIds(_device1ConnectorIds2);

        // Act
        var changeEvent = await tester.TestCommand<DeleteDeviceConnectorIds, DeleteDeviceConnectorIdsConsumer, DeviceConnectorIdsChangedEvent>(command);

        // Assert
        changeEvent.ChangedItems.Should().ContainSingle(k => k.Action == Sdk.Messaging.CrudAction.Deleted && k.Ids.Equals(_device1ConnectorIds2));
    }
}
