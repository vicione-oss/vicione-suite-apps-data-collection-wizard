using DataCollectionWizard.Internal.Contracts;
using DataCollectionWizard.Internal.Services.CloudDataflowGenerators;
using ViciOne.DeviceTree.Contracts;

namespace DataCollectionWizard.Client.Components.ManagementGrid.Models;

/// <summary>
/// Aggregation intervals/functions common to a set of publish targets - what a change scoped to all of them may
/// set. Each target's own offered intervals/functions come from its cloud's <see cref="ICloudFilter"/>
/// (<see cref="PublishTargetInfo.SupportedAggregationIntervals"/>/<see cref="PublishTargetInfo.SupportedAggregationFunctions"/>);
/// this only combines them across targets for the bulk panel, which the single-row editor does not need.
/// </summary>
public static class AggregationOptions
{
    /// <summary>
    /// The intervals every one of <paramref name="targets"/> offers - what a change scoped to all targets may set.
    /// </summary>
    public static IReadOnlyList<AggregationInterval> IntervalsForAll(IEnumerable<PublishTargetInfo> targets)
        => Intersect(targets.Select(t => t.SupportedAggregationIntervals));

    /// <summary>
    /// The functions every one of <paramref name="targets"/> offers - what a change scoped to all targets may set.
    /// </summary>
    public static IReadOnlyList<AggregationFunction> FunctionsForAll(IEnumerable<PublishTargetInfo> targets)
        => Intersect(targets.Select(t => t.SupportedAggregationFunctions));

    // Only values every target supports may be offered for "all targets": writing one a target does not know would
    // leave a configuration the single-row editor could never have produced.
    private static IReadOnlyList<T> Intersect<T>(IEnumerable<IReadOnlyCollection<T>> lists)
    {
        var nonEmpty = lists.Where(list => list.Count > 0).ToList();
        if (nonEmpty.Count == 0)
            return [];

        return [.. nonEmpty[0].Where(value => nonEmpty.TrueForAll(list => list.Contains(value)))];
    }
}
