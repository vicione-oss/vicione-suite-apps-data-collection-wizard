using ViciOne.Driver.IoTCore.Contracts.DeviceTree;

namespace DataCollectionWizard.Public.Extensions;

public static class DataTypeExtensions
{
    public static bool SupportedForCompression(this DataType dataType)
        => dataType switch
        {
            DataType.Float32T => true,
            DataType.IntegerT => true,
            DataType.UIntegerT => true,
            DataType.BooleanT => true,
            _ => false
        };

    public static bool SupportedForLiveView(this DataType dataType)
        => dataType switch
        {
            DataType.BooleanT => true,
            DataType.IntegerT => true,
            DataType.UIntegerT => true,
            DataType.Float32T => true,
            DataType.StringT => true,
            _ => false
        };

    public static bool SupportedForLogging(this DataType dataType)
        => dataType switch
        {
            DataType.Float32T => true,
            DataType.IntegerT => true,
            DataType.UIntegerT => true,
            DataType.BlobT => true,
            DataType.BooleanT => true,
            _ => false
        };
}
