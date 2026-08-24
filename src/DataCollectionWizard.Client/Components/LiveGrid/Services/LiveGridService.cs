using DataCollectionWizard.Client.Components.LiveGrid.Models;
using DataCollectionWizard.Client.Extensions;
using ViciOne.Ui.TreeEditor.Builder;

namespace DataCollectionWizard.Client.Components.LiveGrid.Services;

internal sealed class LiveGridService : IDisposable
{
    private const int RenderIntervalMs = 500;
    private IEnumerable<LiveGridRowModel> _filteredGridItems = [];
    private IEnumerable<LiveGridRowModel> _gridItems = [];
    private string _toolbarSearchText = string.Empty;
    private readonly Timer _renderTimer;
    private int _renderRequested;

    public IEnumerable<LiveGridRowModel> FilteredGridItems => _filteredGridItems;
    public IEnumerable<LiveGridRowModel> GridItems => _gridItems;

    public string ToolbarSearchText
    {
        get => _toolbarSearchText;
        set
        {
            if (value == _toolbarSearchText)
                return;

            _toolbarSearchText = value;
            FilterGridItems(_toolbarSearchText);
        }
    }
    public TreeBuilder TreeBuilder { get; } = new();

    public event Action? FilteredGridItemsChanged;
    public event Action? RebrowseRequested;
    public event Action? RefreshRequested;

    public LiveGridService()
        => _renderTimer = new Timer(OnRenderTick, null, Timeout.Infinite, Timeout.Infinite);

    public void Refresh()
    {
        if (Interlocked.Exchange(ref _renderRequested, 1) == 0)
            _renderTimer.Change(RenderIntervalMs, RenderIntervalMs);
    }

    public void RefreshImmediate()
    {
        Interlocked.Exchange(ref _renderRequested, 0);
        _renderTimer.Change(Timeout.Infinite, Timeout.Infinite);
        RefreshRequested?.Invoke();
    }

    private void OnRenderTick(object? state)
    {
        if (Interlocked.Exchange(ref _renderRequested, 0) == 1)
            RefreshRequested?.Invoke();
        else
            _renderTimer.Change(Timeout.Infinite, Timeout.Infinite);
    }

    public void RequestRebrowse()
        => RebrowseRequested?.Invoke();

    internal void SetGridItems(IEnumerable<LiveGridRowModel> items, bool notify = true)
    {
        if (ReferenceEquals(items, _gridItems))
            return;

        _gridItems = items;
        FilterGridItems(_toolbarSearchText, notify);
    }

    internal void FilterGridItems(string filterText, bool notify = true)
    {
        var filtered = string.IsNullOrWhiteSpace(filterText)
            ? _gridItems
            : [.. _gridItems.Where(gi => gi.DataNode.Name.Contains(filterText, StringComparison.OrdinalIgnoreCase)
                || gi.PathToNode.GetBreadcrumb().Contains(filterText, StringComparison.OrdinalIgnoreCase))];

        if (ReferenceEquals(filtered, _filteredGridItems))
            return;

        _filteredGridItems = filtered;

        if (notify)
            FilteredGridItemsChanged?.Invoke();
    }

    public void Dispose()
        => _renderTimer.Dispose();
}
