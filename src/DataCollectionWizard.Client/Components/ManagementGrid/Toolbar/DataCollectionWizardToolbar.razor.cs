using System.ComponentModel;
using System.Timers;
using DataCollectionWizard.Client.Components.ManagementGrid.Services;
using DataCollectionWizard.Internal.Contracts;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;

namespace DataCollectionWizard.Client.Components.ManagementGrid.Toolbar;

public sealed partial class DataCollectionWizardToolbar : IDisposable
{
    private AggregationInterval _debugAggregationInterval = AggregationInterval.MinutesThirty;
    private readonly System.Timers.Timer _searchBoxTimer = new()
    {
        AutoReset = false,
        Enabled = false,
        Interval = 500,
    };
    private string _searchText = string.Empty;
    private bool _showDebug;

    [CascadingParameter]
    private ManagementGridService Service { get; set; } = default!;

    [Parameter]
    public string SaveReasons { get; set; } = string.Empty;

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

        Service.PropertyChanged -= OnServicePropertyChangedAsync;
    }

    private static string LogLevelToString(LogLevel logLevel)
        => logLevel.ToString();

    protected override void OnInitialized()
    {
        _searchBoxTimer.Elapsed += OnSearchBoxTimerElapsed;
        Service.PropertyChanged += OnServicePropertyChangedAsync;
    }

    private void OnRebrowseClicked()
        => Service.RequestRebrowse();

    private void OnSearchBoxTimerElapsed(object? _1, ElapsedEventArgs _2)
    {
        if (_searchText == Service.ToolbarSearchText)
            return;

        Service.ToolbarSearchText = SearchText;
    }

    private void OnSaveButtonClicked()
        => Service.RequestSave();

    private async void OnServicePropertyChangedAsync(object? _1, PropertyChangedEventArgs e)
    {
        var refresh = e.PropertyName
            is (nameof(ManagementGridService.DeviceTreeChanged))
            or (nameof(ManagementGridService.HasOfflineNodes))
            or (nameof(ManagementGridService.DisableClusterActions));

        if (refresh)
            await InvokeAsync(StateHasChanged);
    }

    private void OnTogglePathVisible()
    {
        Service.PathVisible ^= true;
        Service.Refresh();
    }

    private void OnDeleteOfflineNodesClicked()
        => Service.RequestDeleteOfflineNodes();

    private void SetAllDatapointsCompression(AggregationInterval pg)
        => Service.RequestSetCompressionForAll(pg);

    private void SetAllDatapointsEnabled(bool enabled)
        => Service.RequestSetAllDatapointsEnabled(enabled);

    private void SetDebugRawDataGrid()
        => Service.RequestSetDebugRawData();

    private static LogLevel StringToLogLevel(string str)
        => Enum.TryParse(str, out LogLevel logLevel) ? logLevel : LogLevel.Error;
}
