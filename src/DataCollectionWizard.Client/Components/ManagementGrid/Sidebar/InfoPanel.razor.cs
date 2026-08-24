using System.ComponentModel;
using System.Globalization;
using DataCollectionWizard.Client.Components.ManagementGrid.Services;
using Microsoft.AspNetCore.Components;
using ViciOne.DeviceTree.Contracts;
using ViciOne.DeviceTree.Contracts.Extensions;
using ViciOne.Ui.Localization.Resources;

namespace DataCollectionWizard.Client.Components.ManagementGrid.Sidebar;

// "Informationen" section at the bottom of the device-tree sidebar. Shows a live projection of the value throughput
// the current configuration produces - never a measurement, always recomputed from the config.
public sealed partial class InfoPanel : ComponentBase, IDisposable
{
    private readonly Dictionary<Guid, double> _perCloudPerHour = [];
    private readonly Dictionary<IDeviceTreeMasterNode, double> _perDevicePerHour = [];
    // Ordered contributions (largest first) for the stacked "share of throughput" bars, rebuilt on each Recompute.
    private readonly List<Share> _cloudShares = [];
    private readonly List<Share> _deviceShares = [];
    // Flat cache of the loggable nodes, rebuilt only when the tree structure (root reference) changes; a config
    // change then just re-reads values over this list instead of re-walking the whole tree.
    private readonly List<(IDeviceTreeCompressableDataNode Node, IDeviceTreeMasterNode Master)> _compressables = [];
    private readonly List<IDeviceTreeEventTriggerDataNode> _eventNodes = [];
    private int _cachedTreeVersion = -1;
    private Period _period = Period.Day;
    private double _totalPerHour;
    private double _baselinePerHour;
    private int _onChangeCount;
    private int _eventCount;

    private enum Period { Hour, Day, Week, Month }

    // One contributor (cloud or device) in a stacked share bar: display value, its share % and a stable colour index.
    private sealed record Share(string Name, string Value, int Percent, int ColorIndex);

    [CascadingParameter]
    private ManagementGridService Service { get; set; } = default!;

    private string HeadlineNumber => Format(_totalPerHour * Factor(_period));

    private string HeadlineUnit => $"{CommonVocabulary.ValuePlural} / {PeriodName(_period)}";

    private string VolumePerDay => FormatBytes(_totalPerHour * 24 * 18);

    private string VolumePerMonth => FormatBytes(_totalPerHour * 24 * 30 * 18);

    // The projection differs from the last saved state (the config has unsaved changes).
    private bool HasUnsavedChanges => Service.DeviceTreeChanged && Math.Abs(_totalPerHour - _baselinePerHour) > 0.5;

    private bool IsUnsavedDiffPositive => _totalPerHour >= _baselinePerHour;

    private string UnsavedDiffBadge
    {
        get
        {
            var diff = Math.Abs(_totalPerHour - _baselinePerHour) * Factor(_period);
            // Compact form ("+42M", not "+42,0 Mio.") so the badge stays narrow enough that the label always fits on
            // the same line - a wide badge would wrap the label and make the hero height jump between periods.
            return (IsUnsavedDiffPositive ? "▲ +" : "▼ -") + FormatCompact(diff);
        }
    }

    public void Dispose()
    {
        Service.DataPointEnabledChanged -= OnEnabledChanged;
        Service.BulkEnableApplied -= OnConfigChanged;
        Service.ConfigChanged -= OnConfigChanged;
        Service.PropertyChanged -= OnServicePropertyChanged;
    }

    protected override void OnInitialized()
    {
        Service.DataPointEnabledChanged += OnEnabledChanged;
        Service.BulkEnableApplied += OnConfigChanged;
        Service.ConfigChanged += OnConfigChanged;
        Service.PropertyChanged += OnServicePropertyChanged;
        Recompute();
        _baselinePerHour = _totalPerHour;
    }

    // Track the saved baseline for the "unsaved changes" diff: when the tree becomes clean (saved/reset) the current
    // projection becomes the new baseline. Re-render on any change of the flag so the diff badge appears/disappears.
    private void OnServicePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(ManagementGridService.DeviceTreeChanged))
            return;

        if (!Service.DeviceTreeChanged)
        {
            Recompute();
            _baselinePerHour = _totalPerHour;
        }

        InvokeAsync(StateHasChanged);
    }

    // "Avg Min Max" produces three stored values per sample (Avg, Min, Max); every other aggregation produces one.
    private static int AggregationComponents(AggregationFunction aggregation)
        => aggregation == AggregationFunction.MinMaxAvg ? 3 : 1;

    private static double Factor(Period period) => period switch
    {
        Period.Hour => 1,
        Period.Day => 24,
        Period.Week => 24 * 7,
        Period.Month => 24 * 30,
        _ => 1,
    };

    private static string PeriodName(Period period) => period switch
    {
        Period.Hour => Localization.DataCollectionWizardPage.InfoHour,
        Period.Day => Localization.DataCollectionWizardPage.InfoDay,
        Period.Week => Localization.DataCollectionWizardPage.InfoWeek,
        Period.Month => Localization.DataCollectionWizardPage.InfoMonth,
        _ => string.Empty,
    };

    private static string Format(double value)
    {
        value = Math.Round(value);
        if (value >= 1_000_000)
            return (value / 1_000_000).ToString("0.0", CultureInfo.CurrentCulture) + " Mio.";
        if (value >= 10_000)
            return Math.Round(value / 1000).ToString("0", CultureInfo.CurrentCulture) + "k";
        return value.ToString("#,0", CultureInfo.CurrentCulture);
    }

    // Tighter form of Format for the narrow bar columns: "6,8M" instead of "6,8 Mio." so the value never gets clipped.
    private static string FormatCompact(double value)
    {
        value = Math.Round(value);
        if (value >= 1_000_000)
            return (value / 1_000_000).ToString("0.#", CultureInfo.CurrentCulture) + "M";
        if (value >= 10_000)
            return Math.Round(value / 1000).ToString("0", CultureInfo.CurrentCulture) + "k";
        return value.ToString("#,0", CultureInfo.CurrentCulture);
    }

    private static string FormatBytes(double bytes)
    {
        var megabytes = bytes / 1_000_000;
        return megabytes >= 1000
            ? (megabytes / 1000).ToString("0.0", CultureInfo.CurrentCulture) + " GB"
            : megabytes.ToString("0.0", CultureInfo.CurrentCulture) + " MB";
    }

    private async void OnEnabledChanged(bool _)
        => await InvokeAsync(() => { Recompute(); StateHasChanged(); });

    private async void OnConfigChanged()
        => await InvokeAsync(() => { Recompute(); StateHasChanged(); });

    // Recompute the projection in a single pass over all loggable nodes: throughput per cloud, per device, the total,
    // and the "not projectable" counts (OnChange points and event-triggered raw-data recordings).
    private void Recompute()
    {
        var treeChanged = EnsureNodeCache();

        var targetIds = Service.PublishTargets.Select(target => target.Connection.Id).ToHashSet();

        _perCloudPerHour.Clear();
        _perDevicePerHour.Clear();
        _totalPerHour = 0;
        _onChangeCount = 0;
        _eventCount = 0;

        foreach (var (node, master) in _compressables)
        {
            foreach (var config in node.CompressorConfigurations)
            {
                if (!config.Enabled || !targetIds.Contains(config.DataGroupIdentifier))
                    continue;

                // -1 = OnChange: real rate depends on the signal, so it isn't projectable.
                if (config.CompressionTime == -1)
                {
                    _onChangeCount++;
                    continue;
                }

                if (config.CompressionTime <= 0)
                    continue;

                // CompressionTime is in milliseconds; "Avg Min Max" stores three values per sample.
                var rate = 3_600_000.0 / config.CompressionTime * AggregationComponents(config.Aggregation);
                _perCloudPerHour[config.DataGroupIdentifier] = _perCloudPerHour.GetValueOrDefault(config.DataGroupIdentifier) + rate;
                _perDevicePerHour[master] = _perDevicePerHour.GetValueOrDefault(master) + rate;
                _totalPerHour += rate;
            }
        }

        foreach (var eventNode in _eventNodes)
        {
            _eventCount += eventNode.EventTriggerConfigurations
                .SelectMany(sensor => sensor.Triggers)
                .Count(trigger => trigger.Enabled && (trigger.OnDamage || trigger.OnWarning) && targetIds.Contains(trigger.DataGroupIdentifier));
        }

        RebuildShares();

        // When the tree is in a clean (saved or freshly loaded) state, its projection is the baseline for the
        // unsaved-diff - this also covers the first load when OnInitialized ran before the tree was set (total was 0).
        // A structural edit (e.g. adding a device) also rebuilds the cache but leaves the tree dirty, so we must NOT
        // re-baseline then - otherwise the pending change would be hidden from the diff.
        if (treeChanged && !Service.DeviceTreeChanged)
            _baselinePerHour = _totalPerHour;
    }

    // Rebuild the flat node cache only when the tree structure version changes (load, or an in-place add/remove/
    // rebrowse); returns true when it rebuilt. Config-value changes keep the same version, so the (expensive)
    // structure walk happens once per structural change, not per click.
    private bool EnsureNodeCache()
    {
        if (Service.TreeVersion == _cachedTreeVersion)
            return false;

        _cachedTreeVersion = Service.TreeVersion;
        _compressables.Clear();
        _eventNodes.Clear();

        if (Service.TreeRoot is null)
            return true;

        foreach (var master in Service.TreeRoot.GetNodeAndDescendants().OfType<IDeviceTreeMasterNode>())
        {
            foreach (var node in master.GetNodeAndDescendants())
            {
                if (node is IDeviceTreeCompressableDataNode compressable)
                    _compressables.Add((compressable, master));

                if (node is IDeviceTreeEventTriggerDataNode eventNode)
                    _eventNodes.Add(eventNode);
            }
        }

        return true;
    }

    // Rebuild the ordered contributor lists for the two stacked share bars (clouds and devices).
    private void RebuildShares()
    {
        _cloudShares.Clear();
        _cloudShares.AddRange(ToShares(Service.PublishTargets.Select(target =>
            (target.Connection.Name, _perCloudPerHour.GetValueOrDefault(target.Connection.Id)))));

        _deviceShares.Clear();
        _deviceShares.AddRange(ToShares(_perDevicePerHour.Select(entry => (entry.Key.Name, entry.Value))));
    }

    // Largest contributor first; each gets its per-day value, its rounded share % and its index (drives the colour).
    private static IEnumerable<Share> ToShares(IEnumerable<(string Name, double PerHour)> entries)
    {
        var ordered = entries.OrderByDescending(entry => entry.PerHour).ToList();
        var total = ordered.Sum(entry => entry.PerHour);

        return ordered.Select((entry, index) => new Share(
            entry.Name,
            FormatCompact(entry.PerHour * 24),
            total <= 0 ? 0 : (int)Math.Round(entry.PerHour / total * 100),
            index));
    }

    // 20 evenly-spread hues; the *7 step keeps neighbouring segments far apart in colour. Class-based so no inline style.
    private static string HueClass(int colorIndex)
        => $"hue-{colorIndex * 7 % 20}";

    private static string GrowClass(int percent)
        => $"g-{Math.Clamp(percent, 0, 100)}";

    // The legend shows only the share %; the absolute value is kept here (with its period, so it isn't ambiguous).
    private static string ShareTooltip(Share share)
        => $"{share.Name} · {share.Value}/{Localization.DataCollectionWizardPage.InfoDay} · {share.Percent}%";

    private void SetPeriod(Period period)
        => _period = period;
}
