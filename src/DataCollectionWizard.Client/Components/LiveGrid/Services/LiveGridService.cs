using System.ComponentModel;
using DataCollectionWizard.Client.Components.LiveGrid.Models;
using DataCollectionWizard.Client.Extensions;
using ViciOne.Ui.TreeEditor.Builder;

namespace DataCollectionWizard.Client.Components.LiveGrid.Services;

internal sealed class LiveGridService : INotifyPropertyChanged
{
    private IEnumerable<LiveGridRowModel> _filteredGridItems = [];
    private IEnumerable<LiveGridRowModel> _gridItems = [];
    private string _toolbarSearchText = string.Empty;

    public IEnumerable<LiveGridRowModel> FilteredGridItems
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
    public IEnumerable<LiveGridRowModel> GridItems
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
    public bool PathVisible { get; set; } = true;
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
    public TreeBuilder TreeBuilder { get; } = new();

    public event PropertyChangedEventHandler? PropertyChanged;
    public event Action? RebrowseRequested;
    public event Action? RefreshRequested;

    public void Refresh()
        => RefreshRequested?.Invoke();

    public void RequestRebrowse()
        => RebrowseRequested?.Invoke();

    public void FilterGridItems(string filterText)
    {
        if (string.IsNullOrWhiteSpace(filterText))
        {
            FilteredGridItems = GridItems;
            return;
        }

        FilteredGridItems = [.. GridItems
            .Where(gi =>
                gi.DataNode.Name.Contains(filterText, StringComparison.OrdinalIgnoreCase)
                || gi.PathToNode.GetBreadcrumb().Contains(filterText, StringComparison.OrdinalIgnoreCase)
            )];
    }
}
