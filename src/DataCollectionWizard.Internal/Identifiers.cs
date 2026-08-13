namespace DataCollectionWizard.Internal;

// Restored from ViciOne.Driver.IoTCore.Contracts.Constants.Identifiers, which was removed from the shared
// package when the driver's own raw-data identifiers moved to IoLinkRawDataIdentifier/VseRawDataIdentifier.
// This UI still needs the original DB identifier string format for pooled/aggregated process data.
internal static class Identifiers
{
    private const string DatabaseIdentifierSeparator = "//";

    public static string Recording { get; } = "Recording";
    public static string DeviceTypeIoLink { get; } = "iolink";
    public static string DeviceTypeVse { get; } = "vse";

    public static string GetIoLinkMasterIdentifier(string identifier, string applicationSpecificTag, int port,
        ushort vendorId, uint deviceId, string productName, string processName, string aggregationFunction)
        => $"{DatabaseIdentifierSeparator}{DeviceTypeIoLink}" +
           $"{DatabaseIdentifierSeparator}{identifier}" +
           $"{DatabaseIdentifierSeparator}{applicationSpecificTag}" +
           $"{DatabaseIdentifierSeparator}{port}" +
           $"{DatabaseIdentifierSeparator}{vendorId}" +
           $"{DatabaseIdentifierSeparator}{deviceId}" +
           $"{DatabaseIdentifierSeparator}{productName}" +
           $"{DatabaseIdentifierSeparator}{processName}" +
           $"{DatabaseIdentifierSeparator}{aggregationFunction}";

    public static string GetIoLinkRawDataIdentifier(string identifier, string applicationSpecificTag, int port,
        ushort vendorId, uint deviceId, string productName, string processName, int duration, int frequency)
        => $"{DatabaseIdentifierSeparator}{DeviceTypeIoLink}" +
           $"{DatabaseIdentifierSeparator}{identifier}" +
           $"{DatabaseIdentifierSeparator}{applicationSpecificTag}" +
           $"{DatabaseIdentifierSeparator}{port}" +
           $"{DatabaseIdentifierSeparator}{vendorId}" +
           $"{DatabaseIdentifierSeparator}{deviceId}" +
           $"{DatabaseIdentifierSeparator}{productName}" +
           $"{DatabaseIdentifierSeparator}{processName}" +
           $"{DatabaseIdentifierSeparator}{duration}" +
           $"{DatabaseIdentifierSeparator}{frequency}";

    public static string GetVseAlarmIdentifier(string identifier, string alarmName, string alarmType, string childName, string aggregationFunction)
        => $"{DatabaseIdentifierSeparator}{DeviceTypeVse}" +
           $"{DatabaseIdentifierSeparator}{identifier}" +
           $"{DatabaseIdentifierSeparator}Alarm" +
           $"{DatabaseIdentifierSeparator}Digital" +
           $"{DatabaseIdentifierSeparator}{alarmName}" +
           $"{DatabaseIdentifierSeparator}{alarmType}" +
           $"{DatabaseIdentifierSeparator}{childName}" +
           $"{DatabaseIdentifierSeparator}{aggregationFunction}";

    public static string GetVseCounterIdentifier(string identifier, string counterName, string counterType, string counterUnit, string childName, string aggregationFunction)
        => $"{DatabaseIdentifierSeparator}{DeviceTypeVse}" +
           $"{DatabaseIdentifierSeparator}{identifier}" +
           $"{DatabaseIdentifierSeparator}Counter" +
           $"{DatabaseIdentifierSeparator}{counterName}" +
           $"{DatabaseIdentifierSeparator}{counterType}" +
           $"{DatabaseIdentifierSeparator}{counterUnit}" +
           $"{DatabaseIdentifierSeparator}{childName}" +
           $"{DatabaseIdentifierSeparator}{aggregationFunction}";

    public static string GetVseInputIdentifier(string identifier, string inputType, int inputId, string inputName, string inputUnit, string childName, string aggregationFunction)
        => $"{DatabaseIdentifierSeparator}{DeviceTypeVse}" +
           $"{DatabaseIdentifierSeparator}{identifier}" +
           $"{DatabaseIdentifierSeparator}Input" +
           $"{DatabaseIdentifierSeparator}{inputType}" +
           $"{DatabaseIdentifierSeparator}{inputId}" +
           $"{DatabaseIdentifierSeparator}{inputName}" +
           $"{DatabaseIdentifierSeparator}{inputUnit}" +
           $"{DatabaseIdentifierSeparator}{childName}" +
           $"{DatabaseIdentifierSeparator}{aggregationFunction}";

    public static string GetVseObjectIdentifier(string identifier, string inputType, string inputId, string objectName, string objectId, string objectType, string unit, string childName, string aggregationFunction)
        => $"{DatabaseIdentifierSeparator}{DeviceTypeVse}" +
           $"{DatabaseIdentifierSeparator}{identifier}" +
           $"{DatabaseIdentifierSeparator}Object" +
           $"{DatabaseIdentifierSeparator}{inputType}" +
           $"{DatabaseIdentifierSeparator}{inputId}" +
           $"{DatabaseIdentifierSeparator}{objectName}" +
           $"{DatabaseIdentifierSeparator}{objectId}" +
           $"{DatabaseIdentifierSeparator}{objectType}" +
           $"{DatabaseIdentifierSeparator}{unit}" +
           $"{DatabaseIdentifierSeparator}{childName}" +
           $"{DatabaseIdentifierSeparator}{aggregationFunction}";

    public static string GetVseRawDataIdentifier(string identifier, int sensorIndex, int duration, int samplingRate)
        => $"{DatabaseIdentifierSeparator}{DeviceTypeVse}" +
           $"{DatabaseIdentifierSeparator}{identifier}" +
           $"{DatabaseIdentifierSeparator}RawData" +
           $"{DatabaseIdentifierSeparator}{sensorIndex}" +
           $"{DatabaseIdentifierSeparator}{duration}" +
           $"{DatabaseIdentifierSeparator}{samplingRate}";

    public static string GetVseVariantIdentifier(string identifier, string aggregationFunction)
        => $"{DatabaseIdentifierSeparator}{DeviceTypeVse}" +
           $"{DatabaseIdentifierSeparator}{identifier}" +
           $"{DatabaseIdentifierSeparator}Variant" +
           $"{DatabaseIdentifierSeparator}{aggregationFunction}";
}
