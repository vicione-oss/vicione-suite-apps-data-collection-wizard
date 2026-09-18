using System.ComponentModel;
using System.Globalization;
using DataCollectionWizard.Client.Components.ManagementGrid.Services;
using Microsoft.AspNetCore.Components;
using ViciOne.DeviceTree.Contracts;
using ViciOne.DeviceTree.Contracts.Extensions;
using ViciOne.Ui.Localization.Resources;

namespace DataCollectionWizard.Client.Components.ManagementGrid.Sidebar;

// "Information" section at the bottom of the device-tree sidebar. Shows a live projection of the value throughput
// the current configuration produces - never a measurement, always recomputed from the config.
public sealed partial class InfoPanel : ComponentBase, IDisposable
{
    // How many hues the share bars have to work with; the .hue-* classes are generated to match in the SCSS.
    private const int HueCount = 20;

    private readonly Dictionary<Guid, double> _perCloudPerHour = [];
    private readonly Dictionary<IDeviceTreeMasterNode, double> _perDevicePerHour = [];
    // Ordered contributions (largest first) for the stacked "share of throughput" bars, rebuilt on each Recompute.
    private readonly List<Share> _cloudShares = [];
    private readonly List<Share> _deviceShares = [];
    // A pool per bar, so two clouds get hues as far apart as the palette allows rather than sharing a sequence
    // with the devices.
    private readonly HueAssignment _cloudHues = new();
    private readonly HueAssignment _deviceHues = new();
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
    // One contributor (cloud or device) in a stacked share bar: display value, its share % and the hue it is
    // drawn in - which follows its name, not its current rank. See AssignHues.
    private sealed record Share(string Name, string Value, int Percent, int Hue);

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
        Service.BulkEnableApplied -= OnBulkEnableApplied;
        Service.ConfigChanged -= OnConfigChanged;
        Service.PropertyChanged -= OnServicePropertyChanged;
    }

    protected override void OnInitialized()
    {
        Service.DataPointEnabledChanged += OnEnabledChanged;
        Service.BulkEnableApplied += OnBulkEnableApplied;
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

    // Which cells changed is the grid's business - the panel only needs to know that something did.
    private void OnBulkEnableApplied(Models.BulkChangeHighlight _)
        => OnConfigChanged();

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
            (target.Connection.Name ?? string.Empty, _perCloudPerHour.GetValueOrDefault(target.Connection.Id))), _cloudHues));

        _deviceShares.Clear();
        _deviceShares.AddRange(ToShares(_perDevicePerHour.Select(entry => (entry.Key.Name, entry.Value)), _deviceHues));
    }

    // Largest contributor first; each gets its per-day value, its rounded share % and the hue it is drawn in.
    private static IEnumerable<Share> ToShares(IEnumerable<(string Name, double PerHour)> entries, HueAssignment hues)
    {
        var ordered = entries.OrderByDescending(entry => entry.PerHour).ToList();
        var total = ordered.Sum(entry => entry.PerHour);

        // Names first, in their own order: contributors seen for the first time in this rebuild would otherwise
        // take their hue by how much they happen to produce right now.
        foreach (var name in ordered.Select(entry => entry.Name).Order(StringComparer.Ordinal))
            hues.For(name);

        return ordered.Select(entry => new Share(
            entry.Name,
            FormatCompact(entry.PerHour * 24),
            total <= 0 ? 0 : (int)Math.Round(entry.PerHour / total * 100),
            hues.For(entry.Name)));
    }

    /// <summary>
    /// Remembers which hue a contributor was given, for as long as the panel is open.
    /// </summary>
    /// <remarks>
    /// <para>The colour used to come from the position in the throughput-sorted list, so two clouds swapped
    /// colours the moment one overtook the other - and with 51% against 49% that happens on almost any
    /// configuration change. A colour has to belong to the thing, not to its current rank.</para>
    /// <para>Handing them out in order of first appearance and never taking one back is enough for that, and it
    /// does more than deriving the hue from the name would: removing a contributor leaves every other colour
    /// exactly where it was, and a removed one that comes back gets its old colour again. The hues also stay as
    /// far apart as the palette allows, which a hash cannot promise.</para>
    /// <para>They are not the same colours after a restart. That is deliberate - carrying them across would
    /// mean storing them somewhere, for a legend that names every colour anyway.</para>
    /// </remarks>
    private sealed class HueAssignment
    {
        // Consecutive hues would be near-identical; stepping by 7 through 20 visits every one of them before
        // repeating, and keeps neighbouring contributors far apart in colour.
        private const int Step = 7;

        private readonly Dictionary<string, int> _hues = new(StringComparer.Ordinal);
        private int _handedOut;

        public int For(string name)
        {
            if (_hues.TryGetValue(name, out var hue))
                return hue;

            hue = _handedOut * Step % HueCount;
            _handedOut++;
            _hues[name] = hue;

            return hue;
        }
    }

    private static string HueClass(int hue)
        => $"hue-{hue}";

    private static string GrowClass(int percent)
        => $"grow-{Math.Clamp(percent, 0, 100)}";

    // The legend shows only the share %; the absolute value is kept here (with its period, so it isn't ambiguous).
    private static string ShareTooltip(Share share)
        => $"{share.Name} · {share.Value}/{Localization.DataCollectionWizardPage.InfoDay} · {share.Percent}%";

    private void SetPeriod(Period period)
        => _period = period;
}
