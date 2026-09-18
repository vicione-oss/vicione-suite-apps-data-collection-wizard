using System.Globalization;
using DataCollectionWizard.Client.Components.ManagementGrid.Models;
using Microsoft.AspNetCore.Components;

namespace DataCollectionWizard.Client.Components.ManagementGrid;

/// <summary>
/// One on/off row of the bulk settings panel: the two buttons plus how the selection currently stands.
/// </summary>
/// <remarks>
/// The panel has six of these - process values, uncompressed values, recordings and the three trigger flags -
/// and they only differ in their label and which setting they write.
/// </remarks>
public sealed partial class BulkToggleField : ComponentBase
{
    [Parameter, EditorRequired]
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// How many of the configurations this row writes currently have the flag set.
    /// </summary>
    [Parameter, EditorRequired]
    public BulkTally Tally { get; set; }

    /// <summary>
    /// Raised with the chosen value; the page writes it across the selection.
    /// </summary>
    [Parameter]
    public EventCallback<bool> OnChoose { get; set; }

    // Invariant, because it goes into a CSS width rather than in front of a reader.
    private string SharePercent
        => (Tally.Share * 100).ToString("0.##", CultureInfo.InvariantCulture);

    /// <summary>
    /// The longest the distribution can ever read for this selection, used to hold its width open.
    /// </summary>
    /// <remarks>
    /// Neither figure can exceed the total, so the total in both slots is the widest case - the digits are
    /// tabular, so equal digit counts mean equal width.
    /// </remarks>
    private string WidestText
        => string.Format(CultureInfo.CurrentCulture, Localization.DataCollectionWizardPage.BulkDistribution, Tally.Total, Tally.Total);
}
