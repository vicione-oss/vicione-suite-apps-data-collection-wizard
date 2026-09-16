using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace DataCollectionWizard.Client.Components.ManagementGrid;

public sealed partial class DataPointUsageMeter : ComponentBase, IAsyncDisposable
{
    // Turns amber at this fraction of the recommended limit and red at/above it.
    private const double WarningThreshold = 0.6;

    private ElementReference _hostRef;
    private IJSObjectReference? _countUpModule;

    [Parameter]
    public int Count { get; set; }

    [Parameter]
    public int Limit { get; set; }

    [Inject]
    private IJSRuntime JSRuntime { get; set; } = default!;

    private double Ratio
        => Limit <= 0 ? 0 : (double)Count / Limit;

    private string StateClass
        => Ratio >= 1.0 ? "error" : Ratio >= WarningThreshold ? "warning" : "normal";

    // Fill width driven by a class (no inline style); the classes are generated in the SCSS.
    private string FillClass
        => $"usage-width-{Math.Clamp((int)Math.Round(Ratio * 100), 0, 100)}";

    // Short qualifier shown only when amber/red; the count itself is already in the meter.
    private string? Hint
        => Ratio switch
        {
            >= 1.0 => Localization.DataCollectionWizardPage.DataPointLimitExceededHint,
            >= WarningThreshold => Localization.DataCollectionWizardPage.DataPointLimitApproachingHint,
            _ => null,
        };

    public async ValueTask DisposeAsync()
    {
        try
        {
            if (_countUpModule is not null)
                await _countUpModule.DisposeAsync();
        }
        catch (JSDisconnectedException)
        {
            // The circuit is already gone - nothing left to clean up.
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
            _countUpModule = await JSRuntime.InvokeAsync<IJSObjectReference>("import", CountUp.ModulePath);

        // The bar beside it slides on its own - see the transition on .usage-fill.
        await CountUp.AnimateAsync(_countUpModule, _hostRef);
    }
}
