using System.ComponentModel;
using DataCollectionWizard.Client.Components.ManagementGrid.Services;
using DataCollectionWizard.Internal.Services.CloudDataflowGenerators;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Sdk.Connections.Contracts;
using ViciOne.Driver.IoTCore.Contracts.DeviceTree;

namespace DataCollectionWizard.Client.Components.ManagementGrid;

public sealed partial class DataCollectionWizardGrid : ComponentBase, IDisposable, IAsyncDisposable
{
    private IJSObjectReference? _jsModule;
    private ElementReference _mainContainerRef = default!;
    private bool _resetScrollPositionAfterNextRender;

    [CascadingParameter]
    private ManagementGridService Service { get; set; } = default!;

    [Inject]
    private IJSRuntime JSRuntime { get; set; } = default!;

    [Parameter, EditorRequired]
    public Dictionary<string, IDeviceTreeBase> AllNodes { get; set; } = [];

    [Parameter]
    public EventCallback<(IDeviceTreeSchedulableDataNode BlobDataNode, Connection Configuration)> OnBlobDataDownloadButtonClick { get; set; }

    [Parameter]
    public EventCallback<IDeviceTreeMasterNode> OnDeviceTreeChanged { get; set; }

    [Parameter, EditorRequired]
    public IEnumerable<Connection> PublishTargets { get; set; } = [];

    [Parameter]
    public int RawDataPullingMaxTimesADay { get; set; } = 12;

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

    private static bool IsSupportedConnection(Connection connection)
        => connection is not null && new AnnaCloudFilter().GetCloudConnections([connection]).Any();

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
    }

    private void OnServicePropertyChanged(object? _1, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(ManagementGridService.FilteredGridItems))
            _resetScrollPositionAfterNextRender = true;

        var refresh = e.PropertyName
            is nameof(ManagementGridService.DeviceTreeChanged)
            or nameof(ManagementGridService.FilteredGridItems);

        if (refresh)
            RefreshAsync();
    }

    private async void RefreshAsync()
        => await InvokeAsync(StateHasChanged);
}
