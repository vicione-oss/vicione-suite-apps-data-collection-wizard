using DataCollectionWizard.Client.Services;
using Microsoft.Extensions.DependencyInjection;
using Sdk.Client.Modules;
using Sdk.Client.NavTiles.Extensions;
using ViciOne.Ui.Blazor.Components.ExpandableMenu.Extensions;

namespace DataCollectionWizard.Client;

public sealed class DataCollectionWizardClientModule : ClientModule
{
    public override Action<IServiceCollection>? Configure => (services) =>
    {
        services.AddScoped<IDataCollectionWizardService, DataCollectionWizardService>();
        services.AddScoped<IClusterService, ClusterService>();
        services.AddScoped<IoddImageProvider>();

        services.AddExpandableMenu();

        services.AddNavTiles<DataCollectionWizardClientModule>();
    };
}
