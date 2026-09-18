using DataCollectionWizard.Client.Extensions;
using DataCollectionWizard.Internal.Contracts;
using DataCollectionWizard.Internal.Extensions;
using DataCollectionWizard.Internal.Services.CloudDataflowGenerators;
using DataCollectionWizard.Public.Extensions;
using ViciOne.DeviceTree.Contracts;

namespace DataCollectionWizard.Client.Components.ManagementGrid.Models;

/// <summary>
/// What a grid multi-selection contains and which values its configurations currently share, for the publish
/// targets a bulk change would reach.
/// <para>Kept apart from the grid component so the panel can ask one object instead of the grid knowing every
/// node interface and configuration type itself.</para>
/// </summary>
/// <param name="selectedNodes">The rows currently selected in the grid.</param>
/// <param name="targets">The publish targets in scope - already filtered to those that can be configured.</param>
internal sealed class BulkSelection(IReadOnlyCollection<IDeviceTreeDataNode> selectedNodes, IReadOnlyList<PublishTargetInfo> targets)
{
    // The named weekday sets, expanded once. Doing this per configuration allocated a list per candidate, which a
    // selection of a few hundred recordings turns into thousands of allocations on every render.
    private static readonly (DaysOfWeek Value, DayOfWeek[] Days)[] s_daysOfWeekSets =
        [.. Enum.GetValues<DaysOfWeek>().Select(value => (value, value.AsEnumerable().ToArray()))];

    private BulkTally? _processEnabled;
    private BulkTally? _uncompressedEnabled;
    private BulkTally? _recordingEnabled;
    private BulkTally? _triggerEnabled;
    private BulkTally? _triggerOnDamage;
    private BulkTally? _triggerOnWarning;

    // The capability groups, mirroring how the grid decides which cell to render for a row.
    public IReadOnlyList<IDeviceTreeCompressableDataNode> ProcessNodes { get; } =
        [.. selectedNodes.OfType<IDeviceTreeCompressableDataNode>()
            .Where(node => node.DataType.SupportsLogging && node.DataType.SupportsCompression)];

    public IReadOnlyList<IDeviceTreeCompressableDataNode> UncompressedNodes { get; } =
        [.. selectedNodes.OfType<IDeviceTreeCompressableDataNode>()
            .Where(node => node.DataType.SupportsLogging && !node.DataType.SupportsCompression)];

    public IReadOnlyList<IDeviceTreeSchedulableDataNode> RecordingNodes { get; } =
        [.. selectedNodes.OfType<IDeviceTreeSchedulableDataNode>()];

    public IReadOnlyList<IDeviceTreeEventTriggerDataNode> TriggerNodes { get; } =
        [.. selectedNodes.OfType<IDeviceTreeEventTriggerDataNode>()];

    public IReadOnlyList<IDeviceTreeConfigurableRawDataNode> RawDataNodes { get; } =
        [.. selectedNodes.OfType<IDeviceTreeConfigurableRawDataNode>()];

    /// <summary>
    /// Whether the selection contains anything the panel can offer at all.
    /// </summary>
    public bool HasAnything
        => ProcessNodes.Count > 0 || UncompressedNodes.Count > 0 || RecordingNodes.Count > 0
            || TriggerNodes.Count > 0 || RawDataNodes.Count > 0;

    /// <summary>
    /// How many trigger configurations a change reaches - one per sensor and target, not one per row.
    /// </summary>
    public int TriggerConfigurationCount => Triggers().Count();

    // Tallied rather than reduced to "do they all agree", so the panel can show how the selection currently
    // stands. Each is counted once and kept - the markup asks for them more than once per render, and every ask
    // walks the whole selection.
    public BulkTally ProcessEnabled
        => _processEnabled ??= Tally(CompressorConfigurations(ProcessNodes).Select(configuration => configuration.Enabled));

    public BulkTally UncompressedEnabled
        => _uncompressedEnabled ??= Tally(CompressorConfigurations(UncompressedNodes).Select(configuration => configuration.Enabled));

    public AggregationInterval? Interval
        => Common(CompressorConfigurations(ProcessNodes).Select(configuration => configuration.CompressionTime.ToAggregationInterval()));

    /// <summary>
    /// The shared aggregation function, counting only configurations that are not "On Change" - there the function
    /// is locked in the single-row editor, so including them would show a value that cannot be set here either.
    /// </summary>
    public AggregationFunction? Function
        => Common(CompressorConfigurations(ProcessNodes)
            .Where(configuration => configuration.CompressionTime != (int)AggregationInterval.OnChange)
            .Select(configuration => configuration.Aggregation));

    /// <summary>
    /// How many process configurations in scope are locked to "On Change".
    /// </summary>
    public int LockedOnChangeCount
        => CompressorConfigurations(ProcessNodes).Count(configuration => configuration.CompressionTime == (int)AggregationInterval.OnChange);

    /// <summary>
    /// Whether every process configuration in scope is locked, so the function cannot be set at all.
    /// </summary>
    public bool FunctionFullyLocked
    {
        get
        {
            var all = CompressorConfigurations(ProcessNodes).ToList();
            return all.Count > 0 && all.TrueForAll(configuration => configuration.CompressionTime == (int)AggregationInterval.OnChange);
        }
    }

    public BulkTally RecordingEnabled
        => _recordingEnabled ??= Tally(SchedulerConfigurations().Select(configuration => configuration.Enabled));

    public DaysOfWeek? Days
        => Common(SchedulerConfigurations().Where(configuration => configuration.Times.Count > 0).Select(DaysOfWeekOf));

    public int? TimesADay
        => Common(SchedulerConfigurations().Where(configuration => configuration.Times.Count > 0)
            .Select(configuration => configuration.Times.First().Value.Length));

    public BulkTally TriggerEnabled => _triggerEnabled ??= Tally(Triggers().Select(trigger => trigger.Enabled));

    public BulkTally TriggerOnDamage => _triggerOnDamage ??= Tally(Triggers().Select(trigger => trigger.OnDamage));

    public BulkTally TriggerOnWarning => _triggerOnWarning ??= Tally(Triggers().Select(trigger => trigger.OnWarning));

    public int? TriggerDelay => Common(Triggers().Select(trigger => trigger.Delay));

    public int? RawDataFrequency => Common(RawDataSettings().Select(settings => settings.Frequency));

    public int? RawDataDuration => Common(RawDataSettings().Select(settings => settings.Duration));

    private IEnumerable<CompressorConfiguration> CompressorConfigurations(IEnumerable<IDeviceTreeCompressableDataNode> nodes)
        => from node in nodes
           from target in targets
           let configuration = node.CompressorConfigurations.FirstOrDefault(c => c.DataGroupIdentifier == target.Connection.Id)
           where configuration is not null
           select configuration;

    private IEnumerable<SchedulerConfiguration> SchedulerConfigurations()
        => from node in RecordingNodes
           from target in targets
           let configuration = node.SchedulerConfigurations.FirstOrDefault(c => c.DataGroupIdentifier == target.Connection.Id)
           where configuration is not null
           select configuration;

    private IEnumerable<EventTrigger> Triggers()
        => from node in TriggerNodes
           from sensor in node.EventTriggerConfigurations
           from trigger in sensor.Triggers
           where targets.Any(target => target.Connection.Id == trigger.DataGroupIdentifier)
           select trigger;

    private IEnumerable<RawDataSettings> RawDataSettings()
        => from node in RawDataNodes
           from target in targets
           where node.RawDataConfigurations.ContainsKey(target.Connection.Id)
           select node.RawDataConfigurations[target.Connection.Id];

    /// <summary>
    /// The one value every configuration in scope shares, or <see langword="null"/> when they differ - which the
    /// panel shows as "multiple" rather than presenting one row's value as if it applied to all.
    /// </summary>
    private static BulkTally Tally(IEnumerable<bool> values)
    {
        var onCount = 0;
        var total = 0;

        foreach (var value in values)
        {
            total++;

            if (value)
                onCount++;
        }

        return new BulkTally(onCount, total);
    }

    private static T? Common<T>(IEnumerable<T> values)
        where T : struct
    {
        T? shared = null;

        foreach (var value in values)
        {
            if (shared is null)
                shared = value;
            else if (!EqualityComparer<T>.Default.Equals(shared.Value, value))
                return null;
        }

        return shared;
    }

    // The named weekday set a schedule matches, or Everyday when it matches none - mirrors BlobDataCell's lookup.
    private static DaysOfWeek DaysOfWeekOf(SchedulerConfiguration configuration)
    {
        foreach (var (value, days) in s_daysOfWeekSets)
        {
            if (days.Length == configuration.Times.Count && Array.TrueForAll(days, configuration.Times.ContainsKey))
                return value;
        }

        return DaysOfWeek.Everyday;
    }
}
