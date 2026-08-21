using System.ComponentModel;
using System.Globalization;
using DataCollectionWizard.Client.Components.ManagementGrid.Models;
using DataCollectionWizard.Client.Components.ManagementGrid.Services;
using DataCollectionWizard.Client.Models;
using DataCollectionWizard.Internal.Services.CloudDataflowGenerators;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web.Virtualization;
using Microsoft.JSInterop;
using ViciOne.DeviceTree.Contracts;

namespace DataCollectionWizard.Client.Components.ManagementGrid;

public sealed partial class DataCollectionWizardGrid : ComponentBase, IDisposable, IAsyncDisposable
{
    private IJSObjectReference? _jsModule;
    private ElementReference _mainContainerRef = default!;
    private bool _resetScrollPositionAfterNextRender;
    private Virtualize<IndexedItem<GridDisplayRow>>? _virtualizeRef;
    private List<IndexedItem<GridDisplayRow>> _indexedItems = [];
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

    private string GridColumnsStyle
    {
        get
        {
            var deviceName = "max-content";
            var targets = string.Join(" 80px ", Enumerable.Repeat("334px", PublishTargetInfos.Count));
            var absorption = "1fr";

            var targetsPart = targets.Length > 0 ? $" {targets}" : string.Empty;

            return Service.PathVisible
                ? $"34px max-content 24px {deviceName}{targetsPart} {absorption}"
                : $"34px {deviceName}{targetsPart} {absorption}";
        }
    }

    private string BulkSelectionText
        => string.Format(CultureInfo.CurrentCulture, Localization.DataCollectionWizardPage.BulkSelectionCount, Service.SelectionCount);

    private bool AreAllVisibleSelected
        => Service.FilteredGridItems.Count > 0
            && Service.FilteredGridItems.All(item => Service.IsNodeSelected(item.DataNode));

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
        if (firstRender)
        {
            _jsModule = await JSRuntime.InvokeAsync<IJSObjectReference>("import", $"./_content/{typeof(DataCollectionWizardGrid).Assembly.GetName().Name}/Components/ManagementGrid/{nameof(DataCollectionWizardGrid)}.razor.js");
            await _jsModule.InvokeVoidAsync("DataCollectionWizardGrid.makeDraggable", _bulkGripRef, _bulkBarRef);
        }

        if (_resetScrollPositionAfterNextRender && _jsModule is not null)
        {
            await _jsModule.InvokeVoidAsync("DataCollectionWizardGrid.resetScrollPosition", _mainContainerRef);
            _resetScrollPositionAfterNextRender = false;
        }
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
    // combos in the affected cells re-render with the new state instead of their cached one.
    private async void OnBulkEnableApplied()
        => await InvokeAsync(async () =>
            {
                _rowRenderEpoch++;
                RecomputeActiveCounts();
                StateHasChanged();
                if (_virtualizeRef is not null)
                    await _virtualizeRef.RefreshDataAsync();
            });

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

        var dataNodes = Service.GridItems.Select(item => item.DataNode).ToList();

        foreach (var target in PublishTargetInfos)
        {
            var id = target.Connection.Id;

            var compressorCount = dataNodes.OfType<IDeviceTreeCompressableDataNode>()
                .SelectMany(node => node.CompressorConfigurations)
                .Count(configuration => configuration.DataGroupIdentifier == id && configuration.Enabled);

            var schedulerCount = dataNodes.OfType<IDeviceTreeSchedulableDataNode>()
                .SelectMany(node => node.SchedulerConfigurations)
                .Count(configuration => configuration.DataGroupIdentifier == id && configuration.Enabled);

            // A raw-data recording only produces data when it is enabled AND has a trigger condition (Enabled alone
            // is a no-op) - the same predicate the dataflow generator uses.
            var eventTriggerCount = dataNodes.OfType<IDeviceTreeEventTriggerDataNode>()
                .SelectMany(node => node.EventTriggerConfigurations)
                .SelectMany(sensor => sensor.Triggers)
                .Count(trigger => trigger.DataGroupIdentifier == id && trigger.Enabled && (trigger.OnDamage || trigger.OnWarning));

            _activeCounts[id] = compressorCount + schedulerCount + eventTriggerCount;
        }
    }

    private string ActiveDatapointCountText(PublishTargetInfo target)
        => string.Format(CultureInfo.CurrentCulture, Localization.DataCollectionWizardPage.ActiveDatapointCount, _activeCounts.GetValueOrDefault(target.Connection.Id));

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
        var items = Service.FilteredGridItems;

        // Step 1: runs of consecutive rows sharing the same immediate parent (breadcrumb).
        var parentGroups = new List<(string Key, List<ManagementGridRowModel> Items)>();
        var p = 0;
        while (p < items.Count)
        {
            var key = items[p].Breadcrumb;
            var group = new List<ManagementGridRowModel>();
            while (p < items.Count && items[p].Breadcrumb == key)
            {
                group.Add(items[p]);
                p++;
            }
            parentGroups.Add((key, group));
        }

        // Step 2: emit rows. A parent with more than one point gets a header at the parent level. Consecutive
        // single-point parents that share a grandparent are rolled up under one header at that grandparent (so e.g.
        // many one-value alarms collapse into a single "Alarms" group); a lone single-point parent stays a plain row.
        var rows = new List<GridDisplayRow>();
        var dataRowIndex = 0;
        var g = 0;
        while (g < parentGroups.Count)
        {
            if (parentGroups[g].Items.Count > 1)
            {
                AddGroup(parentGroups[g].Key, parentGroups[g].Items);
                g++;
                continue;
            }

            var rollupKey = ParentPath(parentGroups[g].Key);
            var merged = new List<ManagementGridRowModel>(parentGroups[g].Items);
            var h = g + 1;
            while (h < parentGroups.Count
                   && parentGroups[h].Items.Count == 1
                   && ParentPath(parentGroups[h].Key) == rollupKey)
            {
                merged.AddRange(parentGroups[h].Items);
                h++;
            }

            if (merged.Count > 1)
                AddGroup(rollupKey, merged);
            else
                AddRows(groupKey: null, merged);

            g = h;
        }

        _indexedItems = [.. rows.Select((row, idx) => new IndexedItem<GridDisplayRow>(idx, row))];
        RecomputeActiveCounts();

        void AddGroup(string key, List<ManagementGridRowModel> groupItems)
        {
            rows.Add(new GridDisplayRow { GroupItems = groupItems, GroupLabel = key });
            AddRows(key, groupItems);
        }

        void AddRows(string? groupKey, List<ManagementGridRowModel> groupItems)
        {
            foreach (var item in groupItems)
            {
                rows.Add(new GridDisplayRow
                {
                    GroupRelativePath = groupKey is null ? null : RelativePath(item.Breadcrumb, groupKey),
                    IsEvenDataRow = dataRowIndex % 2 == 0,
                    IsGrouped = groupKey is not null,
                    Item = item,
                });
                dataRowIndex++;
            }
        }
    }

    // The parent path is the breadcrumb without its last " / segment".
    private static string ParentPath(string breadcrumb)
    {
        var idx = breadcrumb.LastIndexOf(" / ", StringComparison.Ordinal);
        return idx > 0 ? breadcrumb[..idx] : breadcrumb;
    }

    // The part of a row's breadcrumb below its group (empty when the row is directly inside the group's container).
    private static string RelativePath(string breadcrumb, string groupKey)
        => breadcrumb.Length > groupKey.Length
            ? breadcrumb[groupKey.Length..].TrimStart(' ', '/')
            : string.Empty;

    private void ToggleSelectAll(bool selected)
        => Service.SetNodesSelected(Service.FilteredGridItems.Select(item => item.DataNode), selected);

    private bool IsGroupSelected(GridDisplayRow header)
        => header.GroupItems!.Count > 0
            && header.GroupItems.All(item => Service.IsNodeSelected(item.DataNode));

    private void ToggleGroup(GridDisplayRow header, bool selected)
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

// A single entry in the virtualized grid body: either a group-header row (GroupLabel set) or a data-point row
// (Item set).
internal sealed record GridDisplayRow
{
    public string? GroupLabel { get; init; }
    public IReadOnlyList<ManagementGridRowModel>? GroupItems { get; init; }
    public ManagementGridRowModel? Item { get; init; }
    public bool IsEvenDataRow { get; init; }
    // True for data rows that sit under a group header - their path is shown by the header, so the row only shows
    // the part of the path below the group (empty when the row is directly under the group's container).
    public bool IsGrouped { get; init; }
    public string? GroupRelativePath { get; init; }
    public bool IsHeader => Item is null;
}

// Raised by the grid's bulk bar; handled by the page which mutates the tree. Target null means "all clouds".
public sealed record BulkEnableRequest(bool Enabled, PublishTargetInfo? Target);
