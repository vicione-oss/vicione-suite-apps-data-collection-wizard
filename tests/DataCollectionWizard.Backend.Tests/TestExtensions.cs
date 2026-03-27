using DataCollectionWizard.Backend.DbContext;
using DataCollectionWizard.Internal.Contracts;

namespace DataCollectionWizard.Backend.Tests;

internal static class TestExtensions
{
    public static async Task SeedDeviceConnectorIds(this IDataCollectionWizardDbContext context, params DeviceConnectorIds[] deviceConnectorIds)
    {
        foreach (var deviceConnectorId in deviceConnectorIds)
        {
            context.DeviceConnectorIds.Add(deviceConnectorId);
        }

        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    public static async Task<DeviceConnectorIds> SeedDeviceConnectorIds(this IDataCollectionWizardDbContext context, string deviceAddress, Guid? treeOutput = null, Guid? triggerInput = null)
    {
        var dbItem = new DeviceConnectorIds
        {
            DeviceAddress = deviceAddress,
            DeviceTreeOutput = treeOutput ?? Guid.NewGuid(),
            TriggerInput = triggerInput ?? Guid.NewGuid()
        };

        context.DeviceConnectorIds.Add(dbItem);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        return dbItem;
    }

    public static async Task<IDataCollectionWizardDbContext> SeedDeviceTree(this IDataCollectionWizardDbContext context, string deviceAddress, string deviceTreeJson)
    {
        context.Devices.Add(new DeviceTreeDbModel { DeviceAddress = deviceAddress, DeviceTreeJson = deviceTreeJson });
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        return context;
    }
}
