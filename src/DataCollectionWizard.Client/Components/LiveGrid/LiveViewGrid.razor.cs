using DataCollectionWizard.Client.Components.LiveGrid.Models;
using DataCollectionWizard.Client.Components.LiveGrid.Services;
using DataCollectionWizard.Client.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web.Virtualization;
using Microsoft.JSInterop;

namespace DataCollectionWizard.Client.Components.LiveGrid;

public sealed partial class LiveViewGrid : ComponentBase, IDisposable, IAsyncDisposable
{
    private IJSObjectReference? _jsModule;
    private ElementReference _mainContainerRef = default!;
    private bool _resetScrollPositionAfterNextRender;
    private Virtualize<IndexedItem<GroupedRow<LiveGridRowModel>>>? _virtualizeRef;
    private List<IndexedItem<GroupedRow<LiveGridRowModel>>> _indexedItems = [];
    private IEnumerable<LiveGridRowModel>? _groupedSource;

    [CascadingParameter]
    private LiveGridService Service { get; set; } = default!;

    private string GridColumnsStyle
    {
        // Mirrors the configuration grid, minus its 34px selection column: the name column sizes to its content,
        // the value columns are fixed, and the last one absorbs what is left.
        //
        // Value and unit are narrow on purpose. The value is set flush right and the unit flush left, so the two
        // meet in the middle and read as one figure - "136,1 °" rather than a number here and a degree sign a
        // hundred pixels away.
        get => "max-content 150px 80px 200px 1fr";
    }

    [Inject]
    private IJSRuntime JSRuntime { get; set; } = default!;

    public void Dispose()
        => Dispose(true);

    private void Dispose(bool disposing)
    {
        if (disposing)
        {
            Service.FilteredGridItemsChanged -= OnFilteredGridItemsChanged;
            Service.RefreshRequested -= RefreshAsync;
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
            _jsModule = await JSRuntime.InvokeAsync<IJSObjectReference>("import", $"./_content/{typeof(LiveViewGrid).Assembly.GetName().Name}/Components/LiveGrid/{nameof(LiveViewGrid)}.razor.js");

        if (_resetScrollPositionAfterNextRender && _jsModule is not null)
        {
            await _jsModule.InvokeVoidAsync("LiveViewGrid.resetScrollPosition", _mainContainerRef);
            _resetScrollPositionAfterNextRender = false;
        }
    }

    protected override void OnInitialized()
    {
        Service.FilteredGridItemsChanged += OnFilteredGridItemsChanged;
        Service.RefreshRequested += RefreshAsync;
        RebuildIndexedItems();
    }

    private void OnFilteredGridItemsChanged()
    {
        RebuildIndexedItems();
        RefreshAsync();
    }

    // Grouped by the same rules as the configuration grid, so a value appears under the same header in both.
    private void RebuildIndexedItems()
    {
        // RefreshRequested fires twice a second with new live values, but the list itself rarely changes -
        // regrouping only pays off when it was actually replaced.
        if (ReferenceEquals(_groupedSource, Service.FilteredGridItems))
            return;

        _groupedSource = Service.FilteredGridItems;

        var rows = GridGrouping.Build([.. Service.FilteredGridItems], item => item.Breadcrumb);

        _indexedItems = [.. rows.Select((row, index) => new IndexedItem<GroupedRow<LiveGridRowModel>>(index, row))];

        // Different values are showing now, so the offset scrolled to in the previous ones means nothing. This
        // belongs here rather than in the changed-handler for the same reason the rebuild does: browsing a node
        // replaces the list without raising that event, so the handler never sees it.
        _resetScrollPositionAfterNextRender = true;
    }

    private async void RefreshAsync()
        => await InvokeAsync(async () =>
        {
            // LiveViewPage installs a new item list with notify:false and only then calls RefreshImmediate, so
            // FilteredGridItemsChanged never fires for a browse. Without this the grid would keep showing the
            // rows from before - or, on the first browse, none at all.
            RebuildIndexedItems();

            StateHasChanged();
            if (_virtualizeRef is not null)
                await _virtualizeRef.RefreshDataAsync();
        });
}
