using Bunit;
using ClusterManagement.Public.DataflowEvents;
using DataCollectionWizard.Client.Components;
using DataCollectionWizard.Client.Services;
using DataCollectionWizard.Client.Tests.Extensions;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Sdk.Client.Modules.Localization;
using ViciOne.Driver.IoTCore.Contracts.DeviceTree;
using ViciOne.Ui.Blazor.Components.ExpandableMenu.Extensions;
using ViciOne.Ui.Blazor.Components.Tooltip.Extensions;

namespace DataCollectionWizard.Client.Tests.Components;

public class DataCollectionWizardPageTests
{
    [Fact]
    public void Init_module_should_register_and_configure_services()
    {
        // Arrange
        using var ctx = new BunitContext();

        ctx.SetupSuiteServicesWithBlazorDx(setup =>
        {
            setup.UseNavigationManager = false;
        });

        var dataCollectionWizardServiceMock = Substitute.For<IDataCollectionWizardService>();
        dataCollectionWizardServiceMock.RequestDeviceTreeAsync().Returns(new DeviceTreeRoot());

        ctx.Services.AddScoped(_ => dataCollectionWizardServiceMock);
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.JSInterop.SetupModule("init", _ => true);
        ctx.Services.AddScoped(_ => Substitute.For<IClusterService>());
        ctx.Services.AddExpandableMenu();
        ctx.Services.AddTooltip();
        ctx.Services.AddScoped(_ => Substitute.For<IClientModuleLocalizer<DataCollectionWizardClientModule>>());
        ctx.Services.AddScoped(_ => Substitute.For<IEventBroker>());

        // Act
        var page = ctx.Render<DataCollectionWizardPage>();

        // Assert
        Assert.NotNull(page);
    }
}
