using AwesomeAssertions;
using Bunit;
using DataCollectionWizard.Client.Components.NavTiles;
using DataCollectionWizard.Internal.Requests;
using NSubstitute;
using Sdk.Client.NavTiles.Components;
using Sdk.Client.NavTiles.Extensions;
using Sdk.Client.NavTiles.Services;
using Sdk.Testing.Client;

namespace DataCollectionWizard.Client.Tests.Components.NavTiles;

public class LiveViewNavTileTests
{
    private static NavTileState GetNavTileState(TestServiceProvider services)
    {
        var navTileRegistry = services.GetService<INavTileRegistry<DataCollectionWizardClientModule>>();
        var registryItem = navTileRegistry!.First(k => k.ComponentType == typeof(LiveViewNavTile));

        return registryItem.State;
    }

    [Fact]
    public void NavTile_should_render()
    {
        // Arrange
        using var ctx = new TestContext();
        var services = ctx.Services;
        ctx.SetupSuiteServices(c =>
        {
            c.ClientMediator.Request<GetDevicesRequest, GetDevicesResponse>(Arg.Any<GetDevicesRequest>())
                .Returns(new GetDevicesResponse());
        });

        services.AddNavTiles<DataCollectionWizardClientModule>();

        // Act
        var page = ctx.RenderComponent<LiveViewNavTile>(p =>
        {
            p.Add(k => k.State, GetNavTileState(services));
        });

        // Assert
        Assert.NotNull(page);
    }

    [Fact]
    public void State_should__be_enabled_if_devices_are_available()
    {
        // Arrange
        using var ctx = new TestContext();
        ctx.SetupSuiteServices(c =>
        {
            c.ClientMediator.Request<GetDevicesRequest, GetDevicesResponse>(Arg.Any<GetDevicesRequest>())
                .Returns(new GetDevicesResponse { Devices = ["Address1", "Address2"] });
        });

        ctx.Services.AddNavTiles<DataCollectionWizardClientModule>();
        var state = GetNavTileState(ctx.Services);

        // Act
        ctx.RenderComponent<LiveViewNavTile>(p =>
        {
            p.Add(k => k.State, state);
        });

        // Assert
        state.Enabled.Should().Be(true);
    }

    [Fact]
    public void State_should_be_disabled_if_no_devices_are_available()
    {
        // Arrange
        using var ctx = new TestContext();
        ctx.SetupSuiteServices(c =>
        {
            c.ClientMediator.Request<GetDevicesRequest, GetDevicesResponse>(Arg.Any<GetDevicesRequest>())
                .Returns(new GetDevicesResponse());
        });

        ctx.Services.AddNavTiles<DataCollectionWizardClientModule>();
        var state = GetNavTileState(ctx.Services);

        // Act
        var page = ctx.RenderComponent<LiveViewNavTile>(p =>
        {
            p.Add(k => k.State, state);
        });

        // Assert
        Assert.NotNull(page);
        state.Enabled.Should().Be(false);
    }
}
