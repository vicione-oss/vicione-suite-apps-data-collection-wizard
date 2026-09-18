using System.ComponentModel;
using DataCollectionWizard.Client.Components.ManagementGrid.Models;
using DataCollectionWizard.Internal.Contracts;
using DataCollectionWizard.Internal.Services.CloudDataflowGenerators;
using Microsoft.Extensions.Logging;
using ViciOne.DeviceTree.Contracts;
using ViciOne.Ui.TreeEditor.Builder;

namespace DataCollectionWizard.Client.Components.ManagementGrid.Services;

internal sealed class ManagementGridService : INotifyPropertyChanged
{
    private bool _deviceTreeChanged;
    private bool _disableClusterActions;
    private IReadOnlyList<ManagementGridRowModel> _filteredGridItems = [];
    private IEnumerable<ManagementGridRowModel> _gridItems = [];
    private bool _hasOfflineNodes;
    private string _toolbarSearchText = string.Empty;
    private IDeviceTreeBase? _treeRoot;
    // Multi-select state: the data nodes the user has ticked in the grid for a bulk action.
    private readonly HashSet<IDeviceTreeDataNode> _selectedNodes = [];

    public bool DeviceTreeChanged
    {
        get => _deviceTreeChanged;
        set
        {
            if (value == _deviceTreeChanged)
                return;

            _deviceTreeChanged = value;
            PropertyChanged?.Invoke(this, new(nameof(DeviceTreeChanged)));
        }
    }

    public LogLevel LogLevel { get; set; } = LogLevel.Error;

    public IReadOnlyList<ManagementGridRowModel> FilteredGridItems
    {
        get => _filteredGridItems;
        private set
        {
            if (ReferenceEquals(value, _filteredGridItems))
                return;

            _filteredGridItems = value;
            PropertyChanged?.Invoke(this, new(nameof(FilteredGridItems)));
        }
    }

    public IEnumerable<ManagementGridRowModel> GridItems
    {
        get => _gridItems;
        set
        {
            if (ReferenceEquals(value, _gridItems))
                return;

            _gridItems = value;
            PropertyChanged?.Invoke(this, new(nameof(GridItems)));
            FilterGridItems(ToolbarSearchText);
        }
    }

    public bool HasOfflineNodes
    {
        get => _hasOfflineNodes;
        set
        {
            if (value == _hasOfflineNodes)
                return;

            _hasOfflineNodes = value;
            PropertyChanged?.Invoke(this, new(nameof(HasOfflineNodes)));
        }
    }


    // The active publish targets (clouds); set by the page. Read by the info panel for its per-cloud projection.
    public IReadOnlyList<PublishTargetInfo> PublishTargets { get; set; } = [];

    // The whole device tree root; set by the page (via SetTree) on every load and every structural change - add,
    // remove, rebrowse. The info panel projects over all devices, not just the grid's (selection-scoped) items.
    public IDeviceTreeBase? TreeRoot
    {
        get => _treeRoot;
        set
        {
            _treeRoot = value;
            // Bump on every (re)assignment - including the same instance after an in-place structural edit (e.g. a
            // device added to Children) - so cache-by-reference consumers still see the change.
            TreeVersion++;
        }
    }

    // Increments whenever TreeRoot is (re)assigned; the info panel cache-invalidates against it.
    public int TreeVersion { get; private set; }

    // Multi-select: the data nodes the user has ticked in the grid for a bulk action.
    public IReadOnlyCollection<IDeviceTreeDataNode> SelectedNodes => _selectedNodes;

    public int SelectionCount => _selectedNodes.Count;

    public bool DisableClusterActions
    {
        get => _disableClusterActions;
        set
        {
            if (value == _disableClusterActions)
                return;

            _disableClusterActions = value;
            PropertyChanged?.Invoke(this, new(nameof(DisableClusterActions)));
        }
    }

    public TreeBuilder TreeBuilder { get; } = new();
    public string ToolbarSearchText
    {
        get => _toolbarSearchText;
        set
        {
            if (value == _toolbarSearchText)
                return;

            _toolbarSearchText = value;
            PropertyChanged?.Invoke(this, new(nameof(ToolbarSearchText)));
            FilterGridItems(_toolbarSearchText);
        }
    }

    public event Action? AddNewIoLinkMasterRequested;
    public event Action? AddNewVseRequested;
    public event Action<BulkChangeHighlight>? BulkEnableApplied;
    public event Action? ConfigChanged;
    public event Action<bool>? DataPointEnabledChanged;
    public event Action? DeleteOfflineNodesRequested;
    public event PropertyChangedEventHandler? PropertyChanged;
    public event Action? RebrowseRequested;
    public event Action? RefreshRequested;
    public event Action? SaveRequested;
    public event Action? SelectionChanged;
    public event Action<bool>? SetAllDatapointsEnabledRequested;
    public event Action<AggregationInterval>? SetCompressionForAllRequested;
    public event Action? SetDebugRawDataRequested;

    public void FilterGridItems(string filterText)
    {
        if (string.IsNullOrWhiteSpace(filterText))
        {
            FilteredGridItems = [.. GridItems];
            return;
        }

        FilteredGridItems = [.. GridItems
            .Where(gi =>
                gi.DataNode.Name.Contains(filterText, StringComparison.OrdinalIgnoreCase)
                || gi.Breadcrumb.Contains(filterText, StringComparison.OrdinalIgnoreCase)
            )];
    }

    public void InvokeDataPointEnabledChanged(bool enabled)
        => DataPointEnabledChanged?.Invoke(enabled);

    // ─── Multi-select ──────────────────────────────────────────────────────
    public void InvokeBulkEnableApplied(BulkChangeHighlight highlight)
        => BulkEnableApplied?.Invoke(highlight);

    public void InvokeConfigChanged()
        => ConfigChanged?.Invoke();

    public bool IsNodeSelected(IDeviceTreeDataNode node)
        => _selectedNodes.Contains(node);

    public void SetNodeSelected(IDeviceTreeDataNode node, bool selected)
    {
        if (selected ? _selectedNodes.Add(node) : _selectedNodes.Remove(node))
            SelectionChanged?.Invoke();
    }

    public void SetNodesSelected(IEnumerable<IDeviceTreeDataNode> nodes, bool selected)
    {
        var changed = false;
        foreach (var node in nodes)
            changed |= selected ? _selectedNodes.Add(node) : _selectedNodes.Remove(node);

        if (changed)
            SelectionChanged?.Invoke();
    }

    public void ClearSelection()
    {
        if (_selectedNodes.Count == 0)
            return;

        _selectedNodes.Clear();
        SelectionChanged?.Invoke();
    }

    public void Refresh()
        => RefreshRequested?.Invoke();

    public void RequestAddNewVse()
        => AddNewVseRequested?.Invoke();

    public void RequestAddIoLinkMaster()
        => AddNewIoLinkMasterRequested?.Invoke();

    public void RequestDeleteOfflineNodes()
        => DeleteOfflineNodesRequested?.Invoke();

    internal void RequestRebrowse()
        => RebrowseRequested?.Invoke();

    public void RequestSave()
        => SaveRequested?.Invoke();

    public void RequestSetAllDatapointsEnabled(bool enabled)
        => SetAllDatapointsEnabledRequested?.Invoke(enabled);

    public void RequestSetCompressionForAll(AggregationInterval aggregationInterval)
        => SetCompressionForAllRequested?.Invoke(aggregationInterval);

    public void RequestSetDebugRawData()
        => SetDebugRawDataRequested?.Invoke();
}
