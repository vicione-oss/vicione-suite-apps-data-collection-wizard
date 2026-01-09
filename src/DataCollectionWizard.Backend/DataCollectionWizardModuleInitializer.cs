using DataCollectionWizard.Backend.DbContext;
using DataCollectionWizard.Backend.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Sdk.Backend.Messaging;
using Sdk.Backend.Modules;
using Sdk.Connections.Commands;

namespace DataCollectionWizard.Backend;

public sealed class DataCollectionWizardModuleInitializer : IModuleInitializer
{
    public async Task Migrate(IServiceProvider scopedServices, CancellationToken stoppingToken = default)
    {
        var dbContext = scopedServices.GetRequiredService<IDataCollectionWizardDbContext>();

        await dbContext.Instance.Database.MigrateAsync(stoppingToken);
    }

    public async Task OnInitialized(IServiceProvider scopedServices, CancellationToken stoppingToken = default)
    {
        var mediator = scopedServices.GetRequiredService<ISuiteMediator>();

        await mediator.Send(new UpsertTag(Public.Constants.AnnaCloud), stoppingToken);
        _ = scopedServices.GetRequiredService<IDeviceTreeGuard>();
    }

    public Task OnPostMigrate(IServiceProvider scopedServices, CancellationToken stoppingToken = default)
        => Task.CompletedTask;

    public Task OnPreMigrate(IServiceProvider scopedServices, CancellationToken stoppingToken = default)
        => Task.CompletedTask;
}
