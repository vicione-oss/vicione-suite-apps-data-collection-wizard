using System.Text.Json;
using System.Text.Json.Serialization;

namespace DataCollectionWizard.Internal;

public static class SerializerOptions
{
    public static readonly JsonSerializerOptions DeviceTree = new() { Converters = { new JsonStringEnumConverter() }, PropertyNameCaseInsensitive = true, ReferenceHandler = ReferenceHandler.Preserve };
}
