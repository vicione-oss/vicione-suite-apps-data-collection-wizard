using DataCollectionWizard.Backend.DbContext;
using DataCollectionWizard.Backend.Services;
using DataCollectionWizard.Internal.Services.CloudDataflowGenerators;
using DataCollectionWizard.Internal.Services.DeviceDataflowGenerators;
using DataCollectionWizard.Public.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Sdk.Backend.Extensions;
using Sdk.Backend.Modules;

namespace DataCollectionWizard.Backend;

public sealed class DataCollectionWizardBackendModule : BackendModule
{
    public override IModuleInitializer ModuleInitializer => new DataCollectionWizardModuleInitializer();

    public override void ConfigureServices(IServiceCollection services, IConfiguration config, IMvcBuilder builder)
    {
        services.AddModuleDbContext<IDataCollectionWizardDbContext, DataCollectionWizardAttributeDbContextSqlite, DataCollectionWizardAttributeDbContextPostgres>(
            this, DataCollectionWizardAttributeDbContext.DbSchemaName, enableSynchronization: false);

        services.AddSingleton<ClusterServiceState>();
        services.AddScoped<IClusterService, ClusterService>();
        services.AddScoped<IConnectionService, ConnectionService>();

        services.AddTransient<IDeviceDataflowGenerator, VseDataflowGenerator>();
        services.AddTransient<IDeviceDataflowGenerator, IoLinkDataflowGenerator>();

        services.AddTransient<ICloudDataflowGenerator, AnnaCloudDataflowGenerator>();
        services.AddTransient<ICloudDataflowGenerator, MoneoCloudDataflowGenerator>();
        services.AddTransient<ICloudDataflowGenerator, MqttCloudDataflowGenerator>();
        services.AddTransient<ICloudDataflowGenerator, OpcUaCloudDataflowGenerator>();

        services.AddTransient<ICloudFilter, AnnaCloudFilter>();
        services.AddTransient<ICloudFilter, MoneoCloudFilter>();
        services.AddTransient<ICloudFilter, MqttCloudFilter>();
        services.AddTransient<ICloudFilter, OpcUaCloudFilter>();

        services.AddSingleton<DataCollectionWizardState>();

        services.AddSingleton<ConnectionChangedProcessorState>();
        services.AddScoped<IConnectionChangedProcessor, ConnectionChangedProcessor>();

        services.AddSingleton<IDeviceTreeGuard, DeviceTreeGuard>();
        services.AddScoped<IDataCollectionWizardService, DataCollectionWizardService>();

        services.AddScoped<IDeviceTreeUpdater, DeviceTreeUpdater>();
    }
}
