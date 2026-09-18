using DataCollectionWizard.Internal.Services.CloudDataflowGenerators;

namespace DataCollectionWizard.Client.Components.ManagementGrid.Models;

/// <summary>
/// A setting the bulk panel can apply to the whole selection at once.
/// <para>Each value belongs to exactly one capability group, so the panel only offers the ones whose group is
/// actually present in the selection. Process values and uncompressed values write the same field
/// (<c>CompressorConfiguration.Enabled</c>) but are kept apart, because they are two visibly different groups in
/// the panel and a user enabling one does not mean the other.</para>
/// <para>A trigger's <c>Enabled</c> is written on its own, without touching its conditions: it is the switch that
/// turns a trigger off while keeping how it was set up, so enabling one that has neither condition set simply
/// restores what was configured - correcting that is not this panel's business.</para>
/// </summary>
public enum BulkSetting
{
    /// <summary>
    /// Compression on/off for compressible process values. Value: <see cref="bool"/>.
    /// </summary>
    ProcessEnabled,

    /// <summary>
    /// The aggregation window. Value: <c>AggregationInterval</c>.
    /// </summary>
    ProcessAggregationInterval,

    /// <summary>
    /// How values inside the window are combined. Value: <c>AggregationFunction</c>.
    /// </summary>
    ProcessAggregationFunction,

    /// <summary>
    /// Logging on/off for values whose data type cannot be compressed. Value: <see cref="bool"/>.
    /// </summary>
    UncompressedEnabled,

    /// <summary>
    /// Scheduled recording on/off. Value: <see cref="bool"/>.
    /// </summary>
    RecordingEnabled,

    /// <summary>
    /// Which weekdays the recording runs on. Value: <c>DaysOfWeek</c>.
    /// </summary>
    RecordingDays,

    /// <summary>
    /// How often per day the recording runs. Value: <see cref="int"/>.
    /// </summary>
    RecordingTimesADay,

    /// <summary>
    /// Event trigger on/off, conditions untouched. Value: <see cref="bool"/>.
    /// </summary>
    TriggerEnabled,

    /// <summary>
    /// Whether the trigger fires on a damage state. Value: <see cref="bool"/>.
    /// </summary>
    TriggerOnDamage,

    /// <summary>
    /// Whether the trigger fires on a warning state. Value: <see cref="bool"/>.
    /// </summary>
    TriggerOnWarning,

    /// <summary>
    /// The trigger's delay in hours. Value: <see cref="int"/>.
    /// </summary>
    TriggerDelay,

    /// <summary>
    /// Sample rate of a raw-data recording, in samples per second. Value: <see cref="int"/>.
    /// </summary>
    RawDataFrequency,

    /// <summary>
    /// Length of a raw-data recording, in milliseconds. Value: <see cref="int"/>.
    /// </summary>
    RawDataDuration,
}

/// <summary>
/// One bulk change requested by the grid's bulk panel and applied by the page, which owns the device tree.
/// </summary>
/// <param name="Setting">Which setting to write; also decides what <paramref name="Value"/> holds.</param>
/// <param name="Target">The publish target to write, or <see langword="null"/> for every configurable one.</param>
/// <param name="Value">The new value, boxed - its runtime type follows from <paramref name="Setting"/>.</param>
public sealed record BulkSettingRequest(BulkSetting Setting, PublishTargetInfo? Target, object Value);
