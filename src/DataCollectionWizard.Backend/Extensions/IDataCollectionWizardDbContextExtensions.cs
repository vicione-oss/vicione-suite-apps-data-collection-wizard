using System.Text.Json;
using DataCollectionWizard.Backend.DbContext;
using DataCollectionWizard.Internal;
using ViciOne.Driver.IoTCore.Contracts.DeviceTree;

namespace DataCollectionWizard.Backend.Extensions;

public static class IDataCollectionWizardDbContextExtensions
{
    public static void UpsertDeviceTree(this IDataCollectionWizardDbContext dbContext, DeviceTreeRoot deviceTree)
    {
        dbContext.Devices.RemoveRange([.. dbContext.Devices]);

        // TODO: andere devicetypen unterstützen, strukturknoten oben drüber anlegen?
        foreach (var masterDevice in deviceTree.Children.OfType<IDeviceTreeMasterNode>())
        {
            var model = new DeviceTreeDbModel
            {
                DeviceAddress = masterDevice.Url.ToString(),
                DeviceTreeJson = JsonSerializer.Serialize(new DeviceTreeStructureNode { Children = [masterDevice], Id = "placeholder", Name = "placeholder" }, SerializerOptions.DeviceTree),
            };

            dbContext.Devices.Add(model);
        }
    }
}
