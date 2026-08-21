using System.Collections.ObjectModel;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using ViciOne.Ui.Blazor.Components.ExpandableMenu.Models;
using ViciOne.Ui.Blazor.Components.Sidebar.Enums;
using ViciOne.Ui.Localization.Resources;
using ViciOne.Ui.MonochromeIcons.Core.Enums;
using ViciOne.Ui.MonochromeIcons.Core.Extensions;

namespace DataCollectionWizard.Client.Components.ManagementGrid.Sidebar;

public sealed partial class DataCollectionWizardSidebar : ComponentBase, IAsyncDisposable
{
    private const int FluidMaximumWidth = 680;
    private const int FluidMinimumWidth = 300;

    private bool _compactMode;
    private DotNetObjectReference<DataCollectionWizardSidebar>? _dotNetRef;
    private ObservableCollection<ExpandableMenuEntry> _entries = [];
    private ElementReference _handleRef;
    private ElementReference _hostRef;
    private IJSObjectReference? _jsModule;
    private int? _sidebarFluidWidth;

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
            $"./_content/{typeof(DataCollectionWizardSidebar).Assembly.GetName().Name}/Components/ManagementGrid/Sidebar/{nameof(DataCollectionWizardSidebar)}.razor.js");

        await _jsModule.InvokeVoidAsync(
            $"{nameof(DataCollectionWizardSidebar)}.attachResize",
            _handleRef,
            _hostRef,
            _dotNetRef,
            FluidMinimumWidth,
            FluidMaximumWidth);
    }

    protected override void OnInitialized()
        => _entries =
        [
            new()
            {
                ContentType = typeof(DeviceTreeSidebarSection),
                IconCssClass = MonochromeIconName.DataflowSolid.GetCssClasses(MonochromeIconSize.SmallMedium).ToSpaceSeparated(),
                IsDefault = true,
                Label = CommonVocabulary.DevicePlural,
            },
            new()
            {
                ContentType = typeof(InfoPanel),
                IconCssClass = MonochromeIconName.InfoOutlined.GetCssClasses(MonochromeIconSize.SmallMedium).ToSpaceSeparated(),
                IsSticky = true,
                Label = Localization.DataCollectionWizardPage.Information,
            },
        ];

    private SidebarMode GetSidebarMode()
        => _compactMode ? SidebarMode.Compact : SidebarMode.Fluid;
}
