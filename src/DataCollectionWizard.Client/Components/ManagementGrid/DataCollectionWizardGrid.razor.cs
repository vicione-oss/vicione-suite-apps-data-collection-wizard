using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using DataCollectionWizard.Client.Components.ManagementGrid.Models;
using DataCollectionWizard.Client.Components.ManagementGrid.Services;
using DataCollectionWizard.Client.Extensions;
using DataCollectionWizard.Client.Models;
using DataCollectionWizard.Internal.Contracts;
using DataCollectionWizard.Internal.Services.CloudDataflowGenerators;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web.Virtualization;
using Microsoft.JSInterop;
using ViciOne.DeviceTree.Contracts;

namespace DataCollectionWizard.Client.Components.ManagementGrid;

public sealed partial class DataCollectionWizardGrid : ComponentBase, IDisposable, IAsyncDisposable
{
    // Kept in step with the highlight animation in DataCollectionWizardGrid.razor.scss.
    private const int HighlightDurationMs = 1100;

    private IJSObjectReference? _jsModule;
    private IJSObjectReference? _countUpModule;
    private ElementReference _mainContainerRef = default!;
    private bool _resetScrollPositionAfterNextRender;
    private Virtualize<IndexedItem<GroupedRow<ManagementGridRowModel>>>? _virtualizeRef;
    private List<IndexedItem<GroupedRow<ManagementGridRowModel>>> _indexedItems = [];
    // Cached per-cloud "N active" header counts, recomputed only on grid-item / enabled-state changes (not per render).
    private readonly Dictionary<Guid, int> _activeCounts = [];
    // Bulk-bar target: 0..PublishTargetInfos.Count-1 selects one cloud, Count means "all clouds".
    private int _bulkTargetIndex;
    // The floating bulk bar and its drag grip - the grip lets the user move the bar out of the way (see the JS module).
    private ElementReference _bulkBarRef;
    private ElementReference _bulkGripRef;
    // Bumped after a bulk enable/disable so the affected rows are re-keyed and re-rendered (their cells otherwise
    // cache their render and would not reflect the externally changed Enabled state).
    private int _rowRenderEpoch;
    // The cells the last bulk change reached, so they can be pointed out; cleared again once the highlight played.
    private BulkChangeHighlight _highlight = BulkChangeHighlight.None;
    // Whether the bulk bar is expanded into its settings panel.
    private bool _bulkPanelOpen;
    // Per-render caches for the bulk panel, cleared again once the render is done - see the Bulk property.
    private BulkSelection? _bulk;
    private List<PublishTargetInfo>? _bulkTargets;

    [CascadingParameter]
    private ManagementGridService Service { get; set; } = default!;

    [Inject]
    private IJSRuntime JSRuntime { get; set; } = default!;

#pragma warning disable CA2227 // Collection properties should be read only - required for Blazor parameter binding
    [Parameter, EditorRequired]
    public Dictionary<string, IDeviceTreeBase> AllNodes { get; set; } = [];
#pragma warning restore CA2227 // Collection properties should be read only - required for Blazor parameter binding

    [Parameter]
    public EventCallback<IDeviceTreeMasterNode> OnDeviceTreeChanged { get; set; }

    [Parameter, EditorRequired]
    public IReadOnlyList<PublishTargetInfo> PublishTargetInfos { get; set; } = [];

    [Parameter]
    public int RawDataPullingMaxTimesADay { get; set; } = 12;

    [Parameter]
    public EventCallback<BulkEnableRequest> OnBulkEnable { get; set; }

    [Parameter]
    public EventCallback<BulkSettingRequest> OnBulkSetting { get; set; }

    [Parameter]
    public EventCallback<BulkResetRequest> OnBulkReset { get; set; }

    // ── bulk settings panel ──────────────────────────────────────────────────
    // The bar grows into the panel rather than opening a second floating element: collapsed it is the bar as
    // before, expanded the settings appear below it. _bulkTargetIndex doubles as the panel's cloud tab, so the
    // bar's dropdown and the tabs are the same choice rather than two that could disagree.

    /// <summary>
    /// The publish target the bulk actions write, or <see langword="null"/> for every configurable one.
    /// </summary>
    private PublishTargetInfo? BulkTarget
        => _bulkTargetIndex >= 0 && _bulkTargetIndex < PublishTargetInfos.Count ? PublishTargetInfos[_bulkTargetIndex] : null;

    /// <summary>
    /// The targets a change actually reaches: the chosen one, or every target that can be configured at all.
    /// A target whose cloud filter supports no node type (moneo today) is never written.
    /// </summary>
    private List<PublishTargetInfo> BulkTargets
        => _bulkTargets ??= BulkTarget is { } target
            ? (target.TreeNodesSupportedForConfiguration.Count > 0 ? [target] : [])
            : [.. PublishTargetInfos.Where(info => info.TreeNodesSupportedForConfiguration.Count > 0)];

    /// <summary>
    /// What the current selection holds and which values it shares, for the targets in scope.
    /// </summary>
    /// <remarks>
    /// Built at most once per render and dropped again in <see cref="OnAfterRenderAsync"/>: the markup asks for it
    /// several times, and each build walks the whole selection five times to sort it into capability groups.
    /// </remarks>
    private BulkSelection Bulk => _bulk ??= new(Service.SelectedNodes, BulkTargets);

    /// <summary>
    /// How many groups the panel will show for the current selection.
    /// </summary>
    /// <remarks>
    /// Decides one against two columns: with a single group the second would stand empty, and the bar should
    /// only be as wide as it has to be.
    /// </remarks>
    private int BulkGroupCount
    {
        get
        {
            var bulk = Bulk;
            var count = 0;

            if (bulk.ProcessNodes.Count > 0)
                count++;

            if (bulk.UncompressedNodes.Count > 0)
                count++;

            // Recordings and raw-data settings share one group.
            if (bulk.RecordingNodes.Count > 0 || bulk.RawDataNodes.Count > 0)
                count++;

            if (bulk.TriggerNodes.Count > 0)
                count++;

            return count;
        }
    }

    /// <summary>
    /// Whether the chosen tab is a target that cannot be configured at all.
    /// </summary>
    private bool BulkTargetUnconfigurable
        => BulkTarget is { } target && target.TreeNodesSupportedForConfiguration.Count == 0;

    /// <summary>
    /// Whether the chosen scope covers targets of differing kinds, so only options common to all can be offered.
    /// </summary>
    private bool BulkOptionsReduced
        => BulkTargets.Select(info => info.Kind).Distinct().Count() > 1;

    private IReadOnlyList<AggregationInterval> BulkIntervals
        => AggregationOptions.IntervalsForAll(BulkTargets);

    private IReadOnlyList<AggregationFunction> BulkFunctions
        => AggregationOptions.FunctionsForAll(BulkTargets);

    /// <summary>
    /// How many times a day a recording may run, matching the single-row editor's upper bound.
    /// </summary>
    private IEnumerable<int> BulkTimesADay => Enumerable.Range(1, Math.Max(1, RawDataPullingMaxTimesADay));

    private static IEnumerable<int> DelayHours
        => Enumerable.Range(RawDataOptions.MinimumDelayHours, RawDataOptions.MaximumDelayHours - RawDataOptions.MinimumDelayHours + 1);

    private string GridColumnsStyle
    {
        get
        {
            var deviceName = "max-content";
            var targets = string.Join(" 80px ", Enumerable.Repeat("334px", PublishTargetInfos.Count));
            var absorption = "1fr";

            var targetsPart = targets.Length > 0 ? $" {targets}" : string.Empty;

            return $"34px {deviceName}{targetsPart} {absorption}";
        }
    }

    private string BulkSelectionText
        => string.Format(CultureInfo.CurrentCulture, Localization.DataCollectionWizardPage.BulkSelectionCount, Service.SelectionCount);

    private bool AreAllVisibleSelected
        => Service.FilteredGridItems.Count > 0
            && Service.FilteredGridItems.All(item => Service.IsNodeSelected(item.DataNode));

    /// <summary>
    /// Some of the visible rows are selected, but not all of them.
    /// </summary>
    /// <remarks>
    /// Drawn as a dash rather than a tick. Without it a partial selection looks exactly like an empty one, so
    /// the box says "nothing is selected" while the bar below it counts 40 rows.
    /// </remarks>
    private bool IsVisibleSelectionMixed
        => !AreAllVisibleSelected
            && Service.FilteredGridItems.Any(item => Service.IsNodeSelected(item.DataNode));

    private string BulkRowCountText(int inGroup)
        => string.Format(CultureInfo.CurrentCulture, Localization.DataCollectionWizardPage.BulkRowCount, inGroup, Service.SelectionCount);

    // Recordings and raw-data settings share one group but not always the same rows: a VSE recording has both
    // halves, an IO-Link BLOB only the schedule. When they differ the header says so, rather than letting the
    // frequency and duration below look as though they applied to every row in the group.
    private string BulkRecordingCountText(int recordings, int withRawData)
        => withRawData > 0 && withRawData != recordings
            ? string.Format(CultureInfo.CurrentCulture, Localization.DataCollectionWizardPage.BulkRowCountWithRawData, recordings, Service.SelectionCount, withRawData)
            : BulkRowCountText(recordings);

    private async Task ApplyBulkSettingAsync(BulkSetting setting, object value)
        => await OnBulkSetting.InvokeAsync(new BulkSettingRequest(setting, BulkTarget, value));

    // Unlike the other bulk actions this discards settings rather than writing one of them, so the page asks
    // first - see OnBulkResetSelection.
    private async Task ApplyBulkResetAsync()
        => await OnBulkReset.InvokeAsync(new BulkResetRequest(BulkTarget));

    // The selects carry an empty value while the selection disagrees; picking that placeholder again must not
    // write anything, so every handler bails on an unparsable value.
    private async Task OnBulkIntervalChangedAsync(ChangeEventArgs args)
    {
        if (Enum.TryParse<AggregationInterval>(args.Value as string, out var interval))
            await ApplyBulkSettingAsync(BulkSetting.ProcessAggregationInterval, interval);
    }

    private async Task OnBulkFunctionChangedAsync(ChangeEventArgs args)
    {
        if (Enum.TryParse<AggregationFunction>(args.Value as string, out var function))
            await ApplyBulkSettingAsync(BulkSetting.ProcessAggregationFunction, function);
    }

    private async Task OnBulkDaysChangedAsync(ChangeEventArgs args)
    {
        if (Enum.TryParse<DaysOfWeek>(args.Value as string, out var days))
            await ApplyBulkSettingAsync(BulkSetting.RecordingDays, days);
    }

    private async Task OnBulkTimesADayChangedAsync(ChangeEventArgs args)
    {
        if (int.TryParse(args.Value as string, NumberStyles.Integer, CultureInfo.InvariantCulture, out var times))
            await ApplyBulkSettingAsync(BulkSetting.RecordingTimesADay, times);
    }

    private async Task OnBulkNumberChangedAsync(BulkSetting setting, ChangeEventArgs args)
    {
        if (int.TryParse(args.Value as string, NumberStyles.Integer, CultureInfo.InvariantCulture, out var number))
            await ApplyBulkSettingAsync(setting, number);
    }

    public void Dispose()
        => Dispose(true);

    private void Dispose(bool disposing)
    {
        if (disposing)
        {
            Service.PropertyChanged -= OnServicePropertyChanged;
            Service.RefreshRequested -= RefreshAsync;
            Service.DataPointEnabledChanged -= OnDataPointEnabledChanged;
            Service.SelectionChanged -= OnSelectionChanged;
            Service.BulkEnableApplied -= OnBulkEnableApplied;
        }
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            if (_jsModule is not null)
                await _jsModule.DisposeAsync();

            if (_countUpModule is not null)
                await _countUpModule.DisposeAsync();
        }
        catch (JSDisconnectedException)
        {
            // This is needed because Blazor server disposes the previous instance only when
            // a new instance is created. This can lead to the catched exception. It does not
            // interfere with the normal functionality of the software.
        }

        Dispose(false);
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        // Drop the per-render caches so the next render reads the current selection and configurations again.
        _bulk = null;
        _bulkTargets = null;

        if (firstRender)
        {
            _jsModule = await JSRuntime.InvokeAsync<IJSObjectReference>("import", $"./_content/{typeof(DataCollectionWizardGrid).Assembly.GetName().Name}/Components/ManagementGrid/{nameof(DataCollectionWizardGrid)}.razor.js");
            await _jsModule.InvokeVoidAsync("DataCollectionWizardGrid.makeDraggable", _bulkGripRef, _bulkBarRef);

            _countUpModule = await JSRuntime.InvokeAsync<IJSObjectReference>("import", CountUp.ModulePath);
        }

        if (_resetScrollPositionAfterNextRender && _jsModule is not null)
        {
            await _jsModule.InvokeVoidAsync("DataCollectionWizardGrid.resetScrollPosition", _mainContainerRef);
            _resetScrollPositionAfterNextRender = false;
        }

        // The per-cloud header counts and the bulk panel's distributions are written by JS, which counts them up
        // to their new value. The bulk bar is a sibling of the grid, not inside it, so both need visiting.
        await CountUp.AnimateAsync(_countUpModule, _mainContainerRef);
        await CountUp.AnimateAsync(_countUpModule, _bulkBarRef);
    }

    protected override void OnInitialized()
    {
        Service.PropertyChanged += OnServicePropertyChanged;
        Service.RefreshRequested += RefreshAsync;
        Service.DataPointEnabledChanged += OnDataPointEnabledChanged;
        Service.SelectionChanged += OnSelectionChanged;
        Service.BulkEnableApplied += OnBulkEnableApplied;
        RebuildIndexedItems();
    }

    // Re-render when the selection changes so the checkboxes, group/all states and the bulk bar stay in sync.
    private async void OnSelectionChanged()
        => await InvokeAsync(StateHasChanged);

    // After a bulk enable/disable, re-key the rows (new epoch) and refresh the virtualized data so the toggles /
    // combos in the affected cells re-render with the new state instead of their cached one. The new key also
    // rebuilds the cell elements, which is what starts the highlight animation on the ones that changed.
    private async void OnBulkEnableApplied(BulkChangeHighlight highlight)
        => await InvokeAsync(async () =>
            {
                _highlight = highlight;
                _rowRenderEpoch++;
                RecomputeActiveCounts();
                StateHasChanged();
                if (_virtualizeRef is not null)
                    await _virtualizeRef.RefreshDataAsync();

                await ClearHighlightAsync();
            });

    // The highlight is a one-off: without this, scrolling a highlighted cell out of view and back would replay
    // the animation, because Virtualize builds a fresh element for it.
    private async Task ClearHighlightAsync()
    {
        var cleared = _highlight;

        await Task.Delay(HighlightDurationMs);

        if (!ReferenceEquals(_highlight, cleared))
            return;

        _highlight = BulkChangeHighlight.None;
        StateHasChanged();
    }

    // The per-cloud "x active" header counts are recomputed on every render. DeviceTreeChanged only raises its
    // event on the first change (its setter short-circuits once dirty), so the counts are kept current from the
    // per-toggle DataPointEnabledChanged signal instead.
    private async void OnDataPointEnabledChanged(bool _)
        => await InvokeAsync(() =>
        {
            RecomputeActiveCounts();
            StateHasChanged();
        });

    // Recompute the per-cloud "N active" header counts and cache them. Previously these were recomputed (and the node
    // list re-materialized per cloud) on EVERY render; now it runs only when the grid items or an enabled state change.
    // Counted across every loggable node whose config carries a per-connection "enabled" flag: compressed/uncompressed
    // process values (CompressorConfigurations), scheduled blob recordings (SchedulerConfigurations) and event-triggered
    // raw-data recordings (only the recording's own Enabled, not the OnDamage/OnWarning trigger conditions).
    private void RecomputeActiveCounts()
    {
        _activeCounts.Clear();
        if (PublishTargetInfos.Count == 0)
            return;

        foreach (var target in PublishTargetInfos)
            _activeCounts[target.Connection.Id] = 0;

        // One pass over the grid, tallying each configuration into its target as it goes. The previous shape ran
        // three LINQ passes per target, so with several clouds a single toggle walked every data point about a
        // dozen times - and this runs on every toggle as well as on every bulk change.
        foreach (var item in Service.GridItems)
        {
            // Sequential ifs, not a switch: a node can implement more than one of these, and each of its
            // configurations counted separately before.
            if (item.DataNode is IDeviceTreeCompressableDataNode compressable)
            {
                foreach (var configuration in compressable.CompressorConfigurations)
                {
                    if (configuration.Enabled)
                        Tally(configuration.DataGroupIdentifier);
                }
            }

            if (item.DataNode is IDeviceTreeSchedulableDataNode schedulable)
            {
                foreach (var configuration in schedulable.SchedulerConfigurations)
                {
                    if (configuration.Enabled)
                        Tally(configuration.DataGroupIdentifier);
                }
            }

            // A raw-data recording only produces data when it is enabled AND has a trigger condition (Enabled alone
            // is a no-op) - the same predicate the dataflow generator uses.
            if (item.DataNode is IDeviceTreeEventTriggerDataNode eventTriggerNode)
            {
                foreach (var sensor in eventTriggerNode.EventTriggerConfigurations)
                {
                    foreach (var trigger in sensor.Triggers)
                    {
                        if (trigger.Enabled && (trigger.OnDamage || trigger.OnWarning))
                            Tally(trigger.DataGroupIdentifier);
                    }
                }
            }
        }

        // A configuration can name a target that has no column here; those were not counted before either.
        void Tally(Guid id)
        {
            ref var count = ref CollectionsMarshal.GetValueRefOrNullRef(_activeCounts, id);

            if (!Unsafe.IsNullRef(ref count))
                count++;
        }
    }


    private void OnServicePropertyChanged(object? _1, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(ManagementGridService.FilteredGridItems))
        {
            _resetScrollPositionAfterNextRender = true;
            _ = InvokeAsync(RebuildIndexedItems);
        }

        var refresh = e.PropertyName
            is nameof(ManagementGridService.DeviceTreeChanged)
            or nameof(ManagementGridService.FilteredGridItems);

        if (refresh)
            RefreshAsync();
    }

    // Weaves group-header rows into the flat data-point list, grouping consecutive rows by breadcrumb (parent path).
    // A header is only emitted for groups with more than one data point - a lone entry already shows its full path,
    // so a header + single row would just be redundant.
    private void RebuildIndexedItems()
    {
        var rows = GridGrouping.Build(Service.FilteredGridItems, item => item.Breadcrumb);

        _indexedItems = [.. rows.Select((row, index) => new IndexedItem<GroupedRow<ManagementGridRowModel>>(index, row))];
        RecomputeActiveCounts();
    }

    private void ToggleSelectAll(bool selected)
        => Service.SetNodesSelected(Service.FilteredGridItems.Select(item => item.DataNode), selected);

    private bool IsGroupSelected(GroupedRow<ManagementGridRowModel> header)
        => header.GroupItems!.Count > 0
            && header.GroupItems.All(item => Service.IsNodeSelected(item.DataNode));

    // Same three states as the select-all box: a group with two of its nine rows ticked said "none" before.
    private bool IsGroupSelectionMixed(GroupedRow<ManagementGridRowModel> header)
        => !IsGroupSelected(header)
            && header.GroupItems!.Any(item => Service.IsNodeSelected(item.DataNode));

    private void ToggleGroup(GroupedRow<ManagementGridRowModel> header, bool selected)
        => Service.SetNodesSelected(header.GroupItems!.Select(item => item.DataNode), selected);

    private async Task ApplyBulkEnableAsync(bool enabled)
    {
        var target = _bulkTargetIndex >= 0 && _bulkTargetIndex < PublishTargetInfos.Count
            ? PublishTargetInfos[_bulkTargetIndex]
            : null; // "all clouds"

        await OnBulkEnable.InvokeAsync(new BulkEnableRequest(enabled, target));
    }

    private async void RefreshAsync()
        => await InvokeAsync(async () =>
            {
                StateHasChanged();
                if (_virtualizeRef is not null)
                    await _virtualizeRef.RefreshDataAsync();
            });
}

// Raised by the grid's bulk bar; handled by the page which mutates the tree. Target null means "all clouds".
public sealed record BulkEnableRequest(bool Enabled, PublishTargetInfo? Target);

/// <summary>
/// Put the selection's settings back to the values a freshly discovered data point is given.
/// </summary>
/// <param name="Target">The publish target to reset, or <see langword="null"/> for every configurable one.</param>
public sealed record BulkResetRequest(PublishTargetInfo? Target);
