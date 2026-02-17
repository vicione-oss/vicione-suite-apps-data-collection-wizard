using AwesomeAssertions;
using Bunit;
using DataCollectionWizard.Client.Components.NavTiles;
using DataCollectionWizard.Internal.Requests;
using NSubstitute;
using Sdk.Client.NavTiles.Components;
using Sdk.Testing.Client;

namespace DataCollectionWizard.Client.Tests.Components.NavTiles;

public class LiveViewNavTileTests
{
    [Fact]
    public void NavTile_should_render()
    {
        // Arrange
        using var ctx = new BunitContext();
        var state = new NavTileState();
        ctx.SetupSuiteServices(c =>
        {
            c.ClientMediator.Request<GetDevicesRequest, GetDevicesResponse>(Arg.Any<GetDevicesRequest>())
                .Returns(new GetDevicesResponse());
        });

        // Act
        var page = ctx.Render<LiveViewNavTile>(p =>
        {
            p.Add(k => k.State, state);
        });

        // Assert
        Assert.NotNull(page);
    }

    [Fact]
    public void State_should__be_enabled_if_devices_are_available()
    {
        // Arrange
        using var ctx = new BunitContext();
        var state = new NavTileState();

        ctx.SetupSuiteServices(c =>
        {
            c.ClientMediator.Request<GetDevicesRequest, GetDevicesResponse>(Arg.Any<GetDevicesRequest>())
                .Returns(new GetDevicesResponse { Devices = ["Address1", "Address2"] });
        });

        // Act
        ctx.Render<LiveViewNavTile>(p =>
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
        using var ctx = new BunitContext();
        var state = new NavTileState();

        ctx.SetupSuiteServices(c =>
        {
            c.ClientMediator.Request<GetDevicesRequest, GetDevicesResponse>(Arg.Any<GetDevicesRequest>())
                .Returns(new GetDevicesResponse());
        });

        // Act
        var page = ctx.Render<LiveViewNavTile>(p =>
        {
            p.Add(k => k.State, state);
        });

        // Assert
        Assert.NotNull(page);
        state.Enabled.Should().Be(false);
    }
}
