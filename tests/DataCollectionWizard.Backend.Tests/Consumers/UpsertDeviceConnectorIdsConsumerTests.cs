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

public class UpsertDeviceConnectorIdsConsumerTests : TestWithDbContextSqlite<DataCollectionWizardAttributeDbContextSqlite>
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
        DeviceAddress = "Address2",
        DeviceTreeOutput = Guid.NewGuid(),
        TriggerInput = Guid.NewGuid(),
    };

    public UpsertDeviceConnectorIdsConsumerTests()
        => _configureServices = cfg =>
            {
                cfg.AddConsumer<UpsertDeviceConnectorIdsConsumer>();
                cfg.AddSingleton<IDataCollectionWizardDbContext>(_ => TestDbContext);
            };

    [Fact]
    public async Task Multiple_not_existing_device_connector_ids_should_be_inserted()
    {
        // Arrange
        await using var tester = new MassTransitTester(_configureServices);
        var command = new UpsertDeviceConnectorIds([_device1ConnectorIds1, _device1ConnectorIds2, _device2ConnectorIds1]);

        // Act
        var changeEvent = await tester.TestCommand<UpsertDeviceConnectorIds, UpsertDeviceConnectorIdsConsumer, DeviceConnectorIdsChangedEvent>(command);

        // Assert
        changeEvent.ChangedItems.Should().HaveCount(3);
        changeEvent.ChangedItems.Should().AllSatisfy(k => k.Action.Should().Be(Sdk.Messaging.CrudAction.Created));
    }


    [Fact]
    public async Task Change_event_should_only_contain_created_device_connector_ids()
    {
        // Arrange
        await using var tester = new MassTransitTester(_configureServices);
        var command = new UpsertDeviceConnectorIds([_device1ConnectorIds1, _device2ConnectorIds1]);

        await tester.Services.GetRequiredService<IDataCollectionWizardDbContext>()
            .SeedDeviceConnectorIds(_device1ConnectorIds1);

        // Act
        var changeEvent = await tester.TestCommand<UpsertDeviceConnectorIds, UpsertDeviceConnectorIdsConsumer, DeviceConnectorIdsChangedEvent>(command);

        // Assert
        changeEvent.ChangedItems.Should().HaveCount(1);
        changeEvent.ChangedItems[0].Action.Should().Be(Sdk.Messaging.CrudAction.Created);
    }

    [Fact]
    public async Task Change_event_should_only_contain_updated_device_connector_ids()
    {
        // Arrange
        await using var tester = new MassTransitTester(_configureServices);
        var command = new UpsertDeviceConnectorIds([_device1ConnectorIds1, _device2ConnectorIds1]);

        await tester.Services.GetRequiredService<IDataCollectionWizardDbContext>()
            .SeedDeviceConnectorIds(_device1ConnectorIds2, _device2ConnectorIds1);

        // Act
        var changeEvent = await tester.TestCommand<UpsertDeviceConnectorIds, UpsertDeviceConnectorIdsConsumer, DeviceConnectorIdsChangedEvent>(command);

        // Assert
        changeEvent.ChangedItems.Should().HaveCount(1);
        changeEvent.ChangedItems[0].Action.Should().Be(Sdk.Messaging.CrudAction.Updated);
    }
}
