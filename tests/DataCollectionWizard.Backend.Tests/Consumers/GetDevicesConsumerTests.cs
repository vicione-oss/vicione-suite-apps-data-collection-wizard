using AwesomeAssertions;
using DataCollectionWizard.Backend.Consumers;
using DataCollectionWizard.Backend.DbContext;
using DataCollectionWizard.Backend.Extensions;
using DataCollectionWizard.Backend.Factories;
using DataCollectionWizard.Internal.Requests;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Sdk.Testing.Backend;

namespace DataCollectionWizard.Backend.Tests.Consumers;

public class GetDevicesConsumerTests : TestWithDbContextSqlite<DataCollectionWizardAttributeDbContextSqlite>
{
    private readonly Action<IBusRegistrationConfigurator> _configureServices;

    public GetDevicesConsumerTests()
        => _configureServices = cfg =>
            {
                cfg.AddConsumer<GetDevicesConsumer>();
                cfg.AddSingleton<IDataCollectionWizardDbContext>(_ => TestDbContext);
            };

    [Fact]
    public async Task Should_return_empty_result_if_no_devices_exist()
    {
        // Arrange
        await using var tester = new MassTransitTester(_configureServices);
        var request = new GetDevicesRequest();

        // Act
        var response = await tester.TestRequest<GetDevicesResponse, GetDevicesRequest>(request);

        // Assert
        response.RequestError.Should().BeNull();
        response.Devices.Should().BeEmpty();
    }

    [Fact]
    public async Task Should_return_all_devices()
    {
        // Arrange
        await using var tester = new MassTransitTester(_configureServices);
        var deviceAddress1 = "Address1";
        var deviceAddress2 = "Address2";
        var deviceTreeJson = FakeDeviceTreeFactory.CreateDeviceTree().SerializeToJson();
        var request = new GetDevicesRequest();

        var context = tester.Services.GetRequiredService<IDataCollectionWizardDbContext>();
        await context.SeedDeviceTree(deviceAddress1, deviceTreeJson);
        await context.SeedDeviceTree(deviceAddress2, deviceTreeJson);

        // Act
        var response = await tester.TestRequest<GetDevicesResponse, GetDevicesRequest>(request);

        // Assert
        response.RequestError.Should().BeNull();
        response.Devices.Should().HaveCount(2);
        response.Devices.Should().ContainSingle(k => k == deviceAddress1);
        response.Devices.Should().ContainSingle(k => k == deviceAddress2);
    }
}
