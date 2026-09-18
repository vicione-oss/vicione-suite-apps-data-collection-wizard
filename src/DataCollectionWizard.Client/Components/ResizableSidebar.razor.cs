using System.Collections.ObjectModel;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using ViciOne.Ui.Blazor.Components.ExpandableMenu.Models;

namespace DataCollectionWizard.Client.Components;

/// <summary>
/// The house <c>Sidebar</c> with a resize that does not stutter.
/// </summary>
/// <remarks>
/// The built-in mover ends its drag on <c>pointerleave</c> and asks .NET to re-render on every move, which on a
/// large device tree is visibly jerky. This wraps the sidebar, hides that mover and drives the width from JS with
/// pointer capture instead, reporting the result back once on release. Both the configuration page and the live
/// view use it - the only thing that differs between them is which menu entries they put inside.
/// </remarks>
public sealed partial class ResizableSidebar : ComponentBase, IAsyncDisposable
{
    private const int FluidMaximumWidth = 680;
    private const int FluidMinimumWidth = 300;

    private bool _compactMode;
    private DotNetObjectReference<ResizableSidebar>? _dotNetRef;
    private ElementReference _handleRef;
    private ElementReference _hostRef;
    private IJSObjectReference? _jsModule;
    private int? _sidebarFluidWidth;

    /// <summary>
    /// The menu entries to show, in order. The first one marked default is the one shown initially.
    /// </summary>
    [Parameter, EditorRequired]
    public ObservableCollection<ExpandableMenuEntry> Entries { get; set; } = [];

    [Inject]
    private IJSRuntime JsRuntime { get; set; } = default!;

    // Called from JS when a resize drag ends: persist the final width so the house Sidebar's bound state matches what
    // the pointer-driven resize left on screen (otherwise the next Blazor render would snap it back).
    [JSInvokable]
    public void SetFluidWidth(int width)
    {
        _sidebarFluidWidth = Math.Clamp(width, FluidMinimumWidth, FluidMaximumWidth);
        StateHasChanged();
    }

    public async ValueTask DisposeAsync()
    {
        _dotNetRef?.Dispose();

        if (_jsModule is not null)
        {
            try
            {
                await _jsModule.DisposeAsync();
            }
            catch (JSDisconnectedException)
            {
                // The circuit is already gone - nothing left to clean up.
            }
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
            return;

        _dotNetRef = DotNetObjectReference.Create(this);
        _jsModule = await JsRuntime.InvokeAsync<IJSObjectReference>(
            "import",
            $"./_content/{typeof(ResizableSidebar).Assembly.GetName().Name}/Components/{nameof(ResizableSidebar)}.razor.js");

        await _jsModule.InvokeVoidAsync(
            $"{nameof(ResizableSidebar)}.attachResize",
            _handleRef,
            _hostRef,
            _dotNetRef,
            FluidMinimumWidth,
            FluidMaximumWidth);
    }
}
