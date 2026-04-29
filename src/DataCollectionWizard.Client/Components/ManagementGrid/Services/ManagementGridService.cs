using System.ComponentModel;
using DataCollectionWizard.Client.Components.ManagementGrid.Models;
using DataCollectionWizard.Internal.Contracts;
using Microsoft.Extensions.Logging;
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

    public bool PathVisible { get; set; } = true;

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
    public event Action<bool>? DataPointEnabledChanged;
    public event Action? DeleteOfflineNodesRequested;
    public event PropertyChangedEventHandler? PropertyChanged;
    public event Action? RebrowseRequested;
    public event Action? RefreshRequested;
    public event Action? SaveRequested;
    public event Action<bool>? SetAllDatapointsEnabledRequested;
    public event Action<PoolingGrid>? SetCompressionForAllRequested;
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

    public void Refresh()
        => RefreshRequested?.Invoke();

    public void RequestAddNewVse()
        => AddNewVseRequested?.Invoke();

    public void ReqestAddIoLinkMaster()
        => AddNewIoLinkMasterRequested?.Invoke();

    public void RequestDeleteOfflineNodes()
        => DeleteOfflineNodesRequested?.Invoke();

    internal void RequestRebrowse()
        => RebrowseRequested?.Invoke();

    public void RequestSave()
        => SaveRequested?.Invoke();

    public void RequestSetAllDatapointsEnabled(bool enabled)
        => SetAllDatapointsEnabledRequested?.Invoke(enabled);

    public void RequestSetCompressionForAll(PoolingGrid poolingGrid)
        => SetCompressionForAllRequested?.Invoke(poolingGrid);

    public void RequestSetDebugRawData()
        => SetDebugRawDataRequested?.Invoke();
}
