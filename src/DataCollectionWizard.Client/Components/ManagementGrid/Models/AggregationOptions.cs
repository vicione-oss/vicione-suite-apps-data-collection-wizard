using DataCollectionWizard.Internal.Contracts;
using DataCollectionWizard.Internal.Services.CloudDataflowGenerators;
using ViciOne.DeviceTree.Contracts;

namespace DataCollectionWizard.Client.Components.ManagementGrid.Models;

/// <summary>
/// Which aggregation intervals and functions a publish target offers.
/// <para>The lists differ per <see cref="ConnectionKind"/> and they are not subsets of one another: only Anna
/// offers <see cref="AggregationFunction.MinMaxAvg"/> and <see cref="AggregationInterval.OnChange"/>, only moneo
/// offers <see cref="AggregationFunction.Last"/>.</para>
/// <para>Both the single-row editor and the bulk panel need this, so it lives here once - while it sat privately
/// in one of them, the other could only have copied it.</para>
/// </summary>
public static class AggregationOptions
{
    private static readonly AggregationInterval[] s_intervalsAnna =
    [
        AggregationInterval.OnChange,
        AggregationInterval.SecondsOne,
        AggregationInterval.SecondsFive,
        AggregationInterval.SecondsTen,
        AggregationInterval.SecondsThirty,
        AggregationInterval.MinutesOne,
        AggregationInterval.MinutesTwo,
        AggregationInterval.MinutesFive,
        AggregationInterval.MinutesTen,
        AggregationInterval.MinutesThirty,
        AggregationInterval.HoursOne,
    ];

    private static readonly AggregationInterval[] s_intervalsMoneo =
    [
        AggregationInterval.SecondsOne,
        AggregationInterval.SecondsTen,
        AggregationInterval.MinutesOne,
    ];

    private static readonly AggregationFunction[] s_functionsAnna =
    [
        AggregationFunction.MinMaxAvg,
        AggregationFunction.Avg,
        AggregationFunction.Min,
        AggregationFunction.Max,
    ];

    private static readonly AggregationFunction[] s_functionsMoneo =
    [
        AggregationFunction.Last,
        AggregationFunction.Avg,
        AggregationFunction.Min,
        AggregationFunction.Max,
    ];

    /// <summary>
    /// The aggregation intervals <paramref name="kind"/> offers, or empty for a kind that cannot be configured.
    /// </summary>
    public static IReadOnlyList<AggregationInterval> IntervalsFor(ConnectionKind kind)
        => kind switch
        {
            ConnectionKind.Anna => s_intervalsAnna,
            ConnectionKind.Moneo => s_intervalsMoneo,
            _ => [],
        };

    /// <summary>
    /// The aggregation functions <paramref name="kind"/> offers, or empty for a kind that cannot be configured.
    /// </summary>
    public static IReadOnlyList<AggregationFunction> FunctionsFor(ConnectionKind kind)
        => kind switch
        {
            ConnectionKind.Anna => s_functionsAnna,
            ConnectionKind.Moneo => s_functionsMoneo,
            _ => [],
        };

    /// <summary>
    /// The intervals every one of <paramref name="kinds"/> offers - what a change scoped to all targets may set.
    /// </summary>
    public static IReadOnlyList<AggregationInterval> IntervalsForAll(IEnumerable<ConnectionKind> kinds)
        => Intersect(kinds, IntervalsFor);

    /// <summary>
    /// The functions every one of <paramref name="kinds"/> offers - what a change scoped to all targets may set.
    /// </summary>
    public static IReadOnlyList<AggregationFunction> FunctionsForAll(IEnumerable<ConnectionKind> kinds)
        => Intersect(kinds, FunctionsFor);

    // Only values every kind supports may be offered for "all targets": writing one a target does not know would
    // leave a configuration the single-row editor could never have produced.
    private static IReadOnlyList<T> Intersect<T>(IEnumerable<ConnectionKind> kinds, Func<ConnectionKind, IReadOnlyList<T>> lookup)
    {
        var lists = kinds.Select(lookup).Where(list => list.Count > 0).ToList();
        if (lists.Count == 0)
            return [];

        return [.. lists[0].Where(value => lists.TrueForAll(list => list.Contains(value)))];
    }
}
