using System.Timers;
using DataCollectionWizard.Client.Components.LiveGrid.Services;
using Microsoft.AspNetCore.Components;

namespace DataCollectionWizard.Client.Components.LiveGrid.Toolbar;

public sealed partial class LiveViewToolbar : IDisposable
{
    private readonly System.Timers.Timer _searchBoxTimer = new()
    {
        AutoReset = false,
        Enabled = false,
        Interval = 500,
    };
    private string _searchText = string.Empty;

    [CascadingParameter]
    private LiveGridService Service { get; set; } = default!;

    private string SearchText
    {
        get => _searchText;
        set
        {
            if (value == _searchText)
                return;

            _searchText = value;
            _searchBoxTimer.Stop();
            _searchBoxTimer.Start();
        }
    }

    public void Dispose()
    {
        _searchBoxTimer.Elapsed -= OnSearchBoxTimerElapsed;
        _searchBoxTimer.Dispose();
    }

    protected override void OnInitialized()
        => _searchBoxTimer.Elapsed += OnSearchBoxTimerElapsed;

    private void OnSearchBoxTimerElapsed(object? _1, ElapsedEventArgs _2)
    {
        if (_searchText == Service.ToolbarSearchText)
            return;

        Service.ToolbarSearchText = SearchText;
    }

    private void OnRefresh()
        => Service.RequestRebrowse();

    private void OnTogglePathVisible()
    {
        Service.PathVisible ^= true;
        Service.Refresh();
    }
}
