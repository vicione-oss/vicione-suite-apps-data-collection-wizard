using System.Text.Json;
using DataCollectionWizard.Internal;
using ViciOne.DeviceTree.Contracts;

namespace DataCollectionWizard.Backend.Extensions;

internal static class DeviceTreeRootExtensions
{
    public static string SerializeToJson(this DeviceTreeRoot deviceTree)
        => JsonSerializer.Serialize(deviceTree, SerializerOptions.DeviceTree);
}
