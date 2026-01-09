using System.Text.Json;
using System.Text.Json.Serialization;

namespace DataCollectionWizard.Internal;

public static class SerializerOptions
{
    public static readonly JsonSerializerOptions DeviceTree = new() { PropertyNameCaseInsensitive = true, ReferenceHandler = ReferenceHandler.Preserve, Converters = { new JsonStringEnumConverter() } };
}
