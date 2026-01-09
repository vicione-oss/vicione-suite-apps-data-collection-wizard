using DataCollectionWizard.Internal.Contracts;

namespace DataCollectionWizard.Internal.Extensions;

public static class PoolingGridHelper
{
    private const string Pooling_HOURS_1 = "1h";
    private const string Pooling_MINUTES_1 = "1min";
    private const string Pooling_MINUTES_10 = "10min";
    private const string Pooling_MINUTES_2 = "2min";
    private const string Pooling_MINUTES_30 = "30min";
    private const string Pooling_MINUTES_5 = "5min";
    private const string Pooling_ONCHANGE = "On Change";
    private const string Pooling_SECONDS_1 = "1s";
    private const string Pooling_SECONDS_10 = "10s";
    private const string Pooling_SECONDS_30 = "30s";
    private const string Pooling_SECONDS_5 = "5s";

    public static string PoolingGridToString(this PoolingGrid poolingGrid)
        => poolingGrid switch
        {
            PoolingGrid.OnChange => Pooling_ONCHANGE,
            PoolingGrid.SecondsOne => Pooling_SECONDS_1,
            PoolingGrid.SecondsFive => Pooling_SECONDS_5,
            PoolingGrid.SecondsTen => Pooling_SECONDS_10,
            PoolingGrid.SecondsThirty => Pooling_SECONDS_30,
            PoolingGrid.MinutesOne => Pooling_MINUTES_1,
            PoolingGrid.MinutesTwo => Pooling_MINUTES_2,
            PoolingGrid.MinutesFive => Pooling_MINUTES_5,
            PoolingGrid.MinutesTen => Pooling_MINUTES_10,
            PoolingGrid.MinutesThirty => Pooling_MINUTES_30,
            PoolingGrid.HoursOne => Pooling_HOURS_1,
            _ => throw new NotImplementedException("Unknown pooling grid.")
        };

    public static PoolingGrid ToPoolingGrid(this string poolingGrid)
        => poolingGrid switch
        {
            Pooling_ONCHANGE => PoolingGrid.OnChange,
            Pooling_SECONDS_1 => PoolingGrid.SecondsOne,
            Pooling_SECONDS_5 => PoolingGrid.SecondsFive,
            Pooling_SECONDS_10 => PoolingGrid.SecondsTen,
            Pooling_SECONDS_30 => PoolingGrid.SecondsThirty,
            Pooling_MINUTES_1 => PoolingGrid.MinutesOne,
            Pooling_MINUTES_2 => PoolingGrid.MinutesTwo,
            Pooling_MINUTES_5 => PoolingGrid.MinutesFive,
            Pooling_MINUTES_10 => PoolingGrid.MinutesTen,
            Pooling_MINUTES_30 => PoolingGrid.MinutesThirty,
            Pooling_HOURS_1 => PoolingGrid.HoursOne,
            _ => throw new NotImplementedException("Unknown pooling grid.")
        };

    public static PoolingGrid ToPoolingGrid(this int poolingGrid)
    {
        if (Enum.IsDefined(typeof(PoolingGrid), poolingGrid))
            return (PoolingGrid)poolingGrid;

        throw new NotImplementedException($"Unknown pooling grid {poolingGrid}.");
    }
}
