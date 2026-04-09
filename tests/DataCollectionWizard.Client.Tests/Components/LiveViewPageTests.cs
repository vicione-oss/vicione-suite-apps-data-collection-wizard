using Bunit;
using ClusterManagement.Public.DataflowEvents;
using DataCollectionWizard.Client.Components;
using DataCollectionWizard.Client.Services;
using DataCollectionWizard.Client.Tests.Extensions;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using ViciOne.Driver.IoTCore.Contracts.DeviceTree;
using ViciOne.Ui.Blazor.Components.Dialog.Extensions;
using ViciOne.Ui.Blazor.Components.ExpandableMenu.Extensions;
using ViciOne.Ui.Blazor.Components.Tooltip.Extensions;

namespace DataCollectionWizard.Client.Tests.Components;

public class LiveViewPageTests
{
    [Fact]
    public async Task Init_module_should_register_and_configure_services()
    {
        // Arrange
        var timeProvider = Substitute.For<TimeProvider>();
        timeProvider.LocalTimeZone.Returns(TimeZoneInfo.Utc);

        await using var ctx = new BunitContext();

        ctx.SetupSuiteServicesWithBlazorDx(setup =>
        {
            setup.UseNavigationManager = false;
        });

        var dataCollectionWizardServiceMock = Substitute.For<IDataCollectionWizardService>();
        dataCollectionWizardServiceMock.RequestDeviceTreeAsync().Returns(new DeviceTreeRoot());

        ctx.Services.AddScoped(_ => dataCollectionWizardServiceMock);
        ctx.Services.AddScoped(_ => Substitute.For<IClusterService>());
        ctx.Services.AddScoped(_ => Substitute.For<IEventBroker>());
        ctx.Services.AddKeyedScoped(Sdk.Constants.ClientTimeProviderServiceKey, (_, __) => timeProvider);

        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.JSInterop.SetupModule("init", _ => true);
        ctx.Services.AddDialog();
        ctx.Services.AddExpandableMenu();
        ctx.Services.AddTooltip();

        // Act
        var page = ctx.Render<LiveViewPage>();

        // Assert
        Assert.NotNull(page);
    }
}
