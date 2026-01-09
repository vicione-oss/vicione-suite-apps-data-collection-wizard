using AwesomeAssertions;
using DataCollectionWizard.Backend.Consumers;
using DataCollectionWizard.Backend.DbContext;
using DataCollectionWizard.Internal.Requests;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Sdk.Testing.Backend;

namespace DataCollectionWizard.Backend.Tests.Consumers;

public class GetDeviceConnectorsConsumerTests : TestWithDbContextSqlite<DataCollectionWizardAttributeDbContextSqlite>
{
    private readonly Action<IBusRegistrationConfigurator> _configureServices;

    public GetDeviceConnectorsConsumerTests()
        => _configureServices = cfg =>
            {
                cfg.AddConsumer<GetDeviceConnectorsConsumer>();
                cfg.AddSingleton<IDataCollectionWizardDbContext>(_ => TestDbContext);
            };

    [Fact]
    public async Task No_device_address_should_return_all_items()
    {
        // Arrange
        await using var tester = new MassTransitTester(_configureServices);
        var request = new GetDeviceConnectorsRequest(null);
        var dbContext = tester.Services.GetRequiredService<IDataCollectionWizardDbContext>();

        dbContext.SeedDeviceConnectorIds("deviceAddress1");
        dbContext.SeedDeviceConnectorIds("deviceAddress2");
        dbContext.SeedDeviceConnectorIds("deviceAddress3");

        // Act
        var response = await tester.TestRequest<GetDeviceConnectorsResponse, GetDeviceConnectorsRequest>(request);

        // Assert
        response.Ids
            .Should()
            .HaveCount(3);
    }

    [Fact]
    public async Task Device_address_should_return_related_items()
    {
        // Arrange
        await using var tester = new MassTransitTester(_configureServices);
        var dbContext = tester.Services.GetRequiredService<IDataCollectionWizardDbContext>();
        var deviceAddress1 = new UriBuilder("deviceAddress1").Uri.ToString();

        var entry1 = dbContext.SeedDeviceConnectorIds(deviceAddress1);
        dbContext.SeedDeviceConnectorIds(new UriBuilder("deviceAddress2").Uri.ToString());
        dbContext.SeedDeviceConnectorIds(deviceAddress1);

        var request = new GetDeviceConnectorsRequest(new UriBuilder(entry1.DeviceAddress).Uri);

        // Act
        var response = await tester.TestRequest<GetDeviceConnectorsResponse, GetDeviceConnectorsRequest>(request);

        // Assert
        response.Ids.Should().HaveCount(2);
        response.Ids.Should().AllSatisfy(x => x.DeviceAddress.Should().Be(deviceAddress1));
    }
}
