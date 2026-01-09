using System.Text.Json;
using DataCollectionWizard.Internal;
using ViciOne.Driver.IoTCore.Contracts.DeviceTree;

namespace DataCollectionWizard.Backend.Extensions;

internal static class DeviceTreeRootExtensions
{
    public static string SerializeToJson(this DeviceTreeRoot deviceTree)
        => JsonSerializer.Serialize(deviceTree, SerializerOptions.DeviceTree);
}
