using System.ComponentModel;
using DataCollectionWizard.Client.Components.ManagementGrid.Models;
using DataCollectionWizard.Client.Components.ManagementGrid.Services;
using DataCollectionWizard.Client.Models;
using DataCollectionWizard.Internal.Services.CloudDataflowGenerators;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web.Virtualization;
using Microsoft.JSInterop;
using ViciOne.Driver.IoTCore.Contracts.DeviceTree;

namespace DataCollectionWizard.Client.Components.ManagementGrid;

public sealed partial class DataCollectionWizardGrid : ComponentBase, IDisposable, IAsyncDisposable
{
    private IJSObjectReference? _jsModule;
    private ElementReference _mainContainerRef = default!;
    private bool _resetScrollPositionAfterNextRender;
    private Virtualize<IndexedItem<ManagementGridRowModel>>? _virtualizeRef;
    private List<IndexedItem<ManagementGridRowModel>> _indexedItems = [];

    [CascadingParameter]
    private ManagementGridService Service { get; set; } = default!;

    [Inject]
    private IJSRuntime JSRuntime { get; set; } = default!;

    [Parameter, EditorRequired]
    public Dictionary<string, IDeviceTreeBase> AllNodes { get; set; } = [];

    [Parameter]
    public EventCallback<IDeviceTreeMasterNode> OnDeviceTreeChanged { get; set; }

    [Parameter, EditorRequired]
    public IReadOnlyList<PublishTargetInfo> PublishTargetInfos { get; set; } = [];

    [Parameter]
    public int RawDataPullingMaxTimesADay { get; set; } = 12;

    private string GridColumnsStyle
    {
        get
        {
            var deviceName = "max-content";
            var targets = string.Join(" 80px ", Enumerable.Repeat("334px", PublishTargetInfos.Count));
            var absorption = "1fr";

            var targetsPart = targets.Length > 0 ? $" {targets}" : string.Empty;

            return Service.PathVisible
                ? $"max-content 24px {deviceName}{targetsPart} {absorption}"
                : $"{deviceName}{targetsPart} {absorption}";
        }
    }

    public void Dispose()
        => Dispose(true);

    private void Dispose(bool disposing)
    {
        if (disposing)
        {
            Service.PropertyChanged -= OnServicePropertyChanged;
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
            _jsModule = await JSRuntime.InvokeAsync<IJSObjectReference>("import", $"./_content/{typeof(DataCollectionWizardGrid).Assembly.GetName().Name}/Components/ManagementGrid/{nameof(DataCollectionWizardGrid)}.razor.js");

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
        RebuildIndexedItems();
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

    private void RebuildIndexedItems()
        => _indexedItems = [.. Service.FilteredGridItems.Select((item, idx) => new IndexedItem<ManagementGridRowModel>(idx, item))];

    private async void RefreshAsync()
        => await InvokeAsync(async () =>
            {
                StateHasChanged();
                if (_virtualizeRef is not null)
                    await _virtualizeRef.RefreshDataAsync();
            });
}
