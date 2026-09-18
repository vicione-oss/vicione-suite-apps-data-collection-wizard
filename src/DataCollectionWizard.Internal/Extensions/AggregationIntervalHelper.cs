using DataCollectionWizard.Internal.Contracts;

namespace DataCollectionWizard.Internal.Extensions;

public static class AggregationIntervalHelper
{
    private const string Interval_HOURS_1 = "1h";
    private const string Interval_MINUTES_1 = "1min";
    private const string Interval_MINUTES_10 = "10min";
    private const string Interval_MINUTES_2 = "2min";
    private const string Interval_MINUTES_30 = "30min";
    private const string Interval_MINUTES_5 = "5min";
    private const string Interval_ONCHANGE = "On Change";
    private const string Interval_SECONDS_1 = "1s";
    private const string Interval_SECONDS_10 = "10s";
    private const string Interval_SECONDS_30 = "30s";
    private const string Interval_SECONDS_5 = "5s";

    public static string AggregationIntervalToString(this AggregationInterval aggregationInterval)
        => aggregationInterval switch
        {
            AggregationInterval.OnChange => Interval_ONCHANGE,
            AggregationInterval.SecondsOne => Interval_SECONDS_1,
            AggregationInterval.SecondsFive => Interval_SECONDS_5,
            AggregationInterval.SecondsTen => Interval_SECONDS_10,
            AggregationInterval.SecondsThirty => Interval_SECONDS_30,
            AggregationInterval.MinutesOne => Interval_MINUTES_1,
            AggregationInterval.MinutesTwo => Interval_MINUTES_2,
            AggregationInterval.MinutesFive => Interval_MINUTES_5,
            AggregationInterval.MinutesTen => Interval_MINUTES_10,
            AggregationInterval.MinutesThirty => Interval_MINUTES_30,
            AggregationInterval.HoursOne => Interval_HOURS_1,
            _ => throw new NotImplementedException("Unknown aggregation interval.")
        };

    public static AggregationInterval ToAggregationInterval(this string aggregationInterval)
        => aggregationInterval switch
        {
            Interval_ONCHANGE => AggregationInterval.OnChange,
            Interval_SECONDS_1 => AggregationInterval.SecondsOne,
            Interval_SECONDS_5 => AggregationInterval.SecondsFive,
            Interval_SECONDS_10 => AggregationInterval.SecondsTen,
            Interval_SECONDS_30 => AggregationInterval.SecondsThirty,
            Interval_MINUTES_1 => AggregationInterval.MinutesOne,
            Interval_MINUTES_2 => AggregationInterval.MinutesTwo,
            Interval_MINUTES_5 => AggregationInterval.MinutesFive,
            Interval_MINUTES_10 => AggregationInterval.MinutesTen,
            Interval_MINUTES_30 => AggregationInterval.MinutesThirty,
            Interval_HOURS_1 => AggregationInterval.HoursOne,
            _ => throw new NotImplementedException("Unknown aggregation interval.")
        };

    public static AggregationInterval ToAggregationInterval(this int aggregationInterval)
    {
        if (Enum.IsDefined(typeof(AggregationInterval), aggregationInterval))
            return (AggregationInterval)aggregationInterval;

        throw new NotImplementedException($"Unknown aggregation interval {aggregationInterval}.");
    }
}
