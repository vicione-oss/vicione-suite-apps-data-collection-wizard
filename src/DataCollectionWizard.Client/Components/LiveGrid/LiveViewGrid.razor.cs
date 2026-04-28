using System.ComponentModel;
using DataCollectionWizard.Client.Components.LiveGrid.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace DataCollectionWizard.Client.Components.LiveGrid;

public sealed partial class LiveViewGrid : ComponentBase, IDisposable, IAsyncDisposable
{
    private IJSObjectReference? _jsModule;
    private ElementReference _mainContainerRef = default!;
    private bool _resetScrollPositionAfterNextRender;

    [CascadingParameter]
    private LiveGridService Service { get; set; } = default!;

    [Inject]
    private IJSRuntime JSRuntime { get; set; } = default!;

    public void Dispose()
        => Dispose(true);

    private void Dispose(bool disposing)
    {
        if (disposing)
        {
            Service.PropertyChanged -= OnServicePropertyChanged;
            Service.RefreshRequested -= RefreshAsync;
            Service.Dispose();
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
        Service.PropertyChanged += OnServicePropertyChanged;
        Service.RefreshRequested += RefreshAsync;
    }

    private void OnServicePropertyChanged(object? s, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(LiveGridService.FilteredGridItems))
        {
            _resetScrollPositionAfterNextRender = true;
            RefreshAsync();
        }
    }

    private async void RefreshAsync()
        => await InvokeAsync(StateHasChanged);
}
