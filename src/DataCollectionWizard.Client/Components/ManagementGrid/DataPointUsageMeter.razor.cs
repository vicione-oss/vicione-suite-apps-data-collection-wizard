using Microsoft.AspNetCore.Components;

namespace DataCollectionWizard.Client.Components.ManagementGrid;

public sealed partial class DataPointUsageMeter
{
    // Turns amber at this fraction of the recommended limit and red at/above it.
    private const double WarningThreshold = 0.6;

    [Parameter]
    public int Count { get; set; }

    [Parameter]
    public int Limit { get; set; }

    private double Ratio
        => Limit <= 0 ? 0 : (double)Count / Limit;

    private string StateClass
        => Ratio >= 1.0 ? "err" : Ratio >= WarningThreshold ? "warn" : "ok";

    // Fill width driven by a class (no inline style); the classes are generated in the SCSS.
    private string FillClass
        => $"dp-w-{Math.Clamp((int)Math.Round(Ratio * 100), 0, 100)}";

    // Short qualifier shown only when amber/red; the count itself is already in the meter.
    private string? Hint
        => Ratio switch
        {
            >= 1.0 => Localization.DataCollectionWizardPage.DataPointLimitExceededHint,
            >= WarningThreshold => Localization.DataCollectionWizardPage.DataPointLimitApproachingHint,
            _ => null,
        };
}
