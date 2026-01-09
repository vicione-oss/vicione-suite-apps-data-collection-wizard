using DataCollectionWizard.Backend.DbContext;
using DataCollectionWizard.Internal.Contracts;

namespace DataCollectionWizard.Backend.Tests;
internal static class TestExtensions
{
    public static void SeedDeviceConnectorIds(this IDataCollectionWizardDbContext context, params DeviceConnectorIds[] deviceConnectorIds)
    {
        foreach (var deviceConnectorId in deviceConnectorIds)
        {
            context.DeviceConnectorIds.Add(deviceConnectorId);
        }

        context.Instance.SaveChanges();
    }

    public static DeviceConnectorIds SeedDeviceConnectorIds(this IDataCollectionWizardDbContext context, string deviceAddress, Guid? treeOutput = null, Guid? triggerInput = null)
    {
        var dbItem = new DeviceConnectorIds
        {
            DeviceAddress = deviceAddress,
            DeviceTreeOutput = treeOutput ?? Guid.NewGuid(),
            TriggerInput = triggerInput ?? Guid.NewGuid()
        };

        context.DeviceConnectorIds.Add(dbItem);
        context.Instance.SaveChanges();

        return dbItem;
    }

    public static IDataCollectionWizardDbContext SeedDeviceTree(this IDataCollectionWizardDbContext context, string deviceAddress, string deviceTreeJson)
    {
        context.Devices.Add(new DeviceTreeDbModel { DeviceAddress = deviceAddress, DeviceTreeJson = deviceTreeJson });
        context.Instance.SaveChanges();

        return context;
    }
}
