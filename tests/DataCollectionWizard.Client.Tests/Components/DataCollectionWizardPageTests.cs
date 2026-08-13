using Bunit;
using ClusterManagement.Public.DataflowEvents;
using ClusterManagement.Public.Services;
using DataCollectionWizard.Client.Components;
using DataCollectionWizard.Client.Services;
using Microsoft.Extensions.DependencyInjection;
using Sdk.Client.Modules.Localization;
using Sdk.Testing.Client;
using ViciOne.DeviceTree.Contracts;
using ViciOne.Ui.Blazor.Components.Dialog.Extensions;
using ViciOne.Ui.Blazor.Components.ExpandableMenu.Extensions;
using ViciOne.Ui.Blazor.Components.Tooltip.Extensions;
using ViciOne.Ui.MonochromeIcons.Assets.Services;

namespace DataCollectionWizard.Client.Tests.Components;

public class DataCollectionWizardPageTests
{
    [Fact]
    public async Task Init_module_should_register_and_configure_services()
    {
        // Arrange
        await using var ctx = new BunitContext();
        ctx.SetupSuiteServices(setup =>
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
        ctx.Services.AddScoped(_ => Substitute.For<IResourceDownloadStateService>());
        ctx.Services.AddScoped(_ => Substitute.For<IMonochromeIconSvgMarkupProvider>());
        ctx.Services.AddScoped<DeviceTreeNodeIconProvider>();
        ctx.Services.AddDialog();

        // Act
        var page = ctx.Render<DataCollectionWizardPage>();

        // Assert
        Assert.NotNull(page);
    }
}
