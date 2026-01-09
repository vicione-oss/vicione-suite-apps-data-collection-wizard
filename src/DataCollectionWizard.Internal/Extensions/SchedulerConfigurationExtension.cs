using ViciOne.Driver.IoTCore.Contracts.DeviceTree;

namespace DataCollectionWizard.Internal.Extensions;

public static class SchedulerConfigurationExtension
{
    private const string TimeEntriesSeparator = ";";
    private const string TimePairSeparator = "#";

    public static string ToFbSetting(this SchedulerConfiguration configuration)
    {
        List<string> entries = [];

        foreach (var entry in configuration.Times)
        {
            foreach (var time in entry.Value)
                entries.Add($"{(int)entry.Key}{TimePairSeparator}{time:hh\\:mm}");
        }

        return string.Join(TimeEntriesSeparator, entries);
    }
}
