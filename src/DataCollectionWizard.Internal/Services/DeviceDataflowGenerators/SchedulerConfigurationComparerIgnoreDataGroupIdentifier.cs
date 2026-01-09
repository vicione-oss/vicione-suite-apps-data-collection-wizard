using System.Globalization;
using ViciOne.Driver.IoTCore.Contracts.DeviceTree;

namespace DataCollectionWizard.Internal.Services.DeviceDataflowGenerators;

public class SchedulerConfigurationComparerIgnoreDataGroupIdentifier : IEqualityComparer<SchedulerConfiguration>
{
    public static readonly SchedulerConfigurationComparerIgnoreDataGroupIdentifier Instance = new();

    private static bool AreTimesEqual(Dictionary<DayOfWeek, TimeSpan[]> x, Dictionary<DayOfWeek, TimeSpan[]> y)
    {
        if (x.Count != y.Count)
            return false;

        foreach (var day in x.Keys)
        {
            if (!y.TryGetValue(day, out var value))
                return false;

            if (x[day].Length != value.Length)
                return false;

            for (var i = 0; i < x[day].Length; i++)
            {
                if (x[day][i] != value[i])
                    return false;
            }
        }

        return true;
    }

    public bool Equals(SchedulerConfiguration? x, SchedulerConfiguration? y)
    {
        if (x is null && y is null)
            return true;

        if (x is null || y is null)
            return false;

        return x.Enabled == y.Enabled
            && AreTimesEqual(x.Times, y.Times);
    }

    public int GetHashCode(SchedulerConfiguration obj)
    {
        var helperString = obj.Enabled.ToString();

        foreach (var day in obj.Times)
        {
            helperString += ((int)day.Key).ToString(CultureInfo.InvariantCulture);

            foreach (var timeOfDay in day.Value)
            {
                helperString += $"{timeOfDay.TotalSeconds.ToString(CultureInfo.InvariantCulture)}|";
            }
        }

        return helperString.GetHashCode(StringComparison.Ordinal);
    }
}
