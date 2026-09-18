using AwesomeAssertions;
using DataCollectionWizard.Backend.Consumers;
using DataCollectionWizard.Backend.Factories;
using DataCollectionWizard.Backend.Services;
using DataCollectionWizard.Public.Requests;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Sdk.Testing.Backend;

namespace DataCollectionWizard.Backend.Tests.Consumers;

public class GetDeviceTreeConsumerTests
{
    private readonly Action<IBusRegistrationConfigurator> _configureServices;
    private readonly IDataCollectionWizardService _dataCollectionWizardService = Substitute.For<IDataCollectionWizardService>();

    public GetDeviceTreeConsumerTests()
        => _configureServices = cfg =>
            {
                cfg.AddConsumer<GetDeviceTreeConsumer>();
                cfg.AddSingleton(_ => _dataCollectionWizardService);
            };

    [Fact]
    public async Task Should_return_device_tree_json()
    {
        // Arrange
        await using var tester = new MassTransitTester(_configureServices);
        var deviceTree = FakeDeviceTreeFactory.CreateDeviceTree();
        var request = new GetDeviceTree();

        _dataCollectionWizardService.RequestDeviceTreeAsync(Arg.Any<CancellationToken>()).Returns(deviceTree);

        // Act
        var response = await tester.TestRequest<GetDeviceTreeResponse, GetDeviceTree>(request);

        // Assert
        response.DeviceTree.Should().NotBeNull();
        response.DeviceTree.Should().BeEquivalentTo(deviceTree);
    }
}

