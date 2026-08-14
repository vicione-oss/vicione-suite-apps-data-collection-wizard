using Microsoft.AspNetCore.Components;

namespace DataCollectionWizard.Client.Components.DeviceScan;

/// <summary>
/// The search box and rescan button above a <see cref="DeviceScanList{TDevice}"/>. It sits next to the dialog's
/// tab strip rather than above the list itself, which is why it is a component of its own.
/// </summary>
public sealed partial class DeviceScanToolbar : ComponentBase
{
    /// <summary>
    /// The text the scan results are filtered by.
    /// </summary>
    [Parameter]
    public string Filter { get; set; } = string.Empty;

    /// <summary>
    /// Raised while the user types in the search box.
    /// </summary>
    [Parameter]
    public EventCallback<string> FilterChanged { get; set; }

    /// <summary>
    /// Raised when the user asks for the scan to run again.
    /// </summary>
    [Parameter]
    public EventCallback OnRescan { get; set; }

    /// <summary>
    /// The rescan button's tooltip.
    /// </summary>
    [Parameter, EditorRequired]
    public string RescanTitle { get; set; } = string.Empty;

    /// <summary>
    /// What the search box binds to. Writing it reports the new filter to the owner, which the plain
    /// <see cref="Filter"/> parameter cannot do on its own.
    /// </summary>
    private string FilterValue
    {
        get => Filter;
        set
        {
            if (string.Equals(value, Filter, StringComparison.Ordinal))
                return;

            Filter = value;
            _ = FilterChanged.InvokeAsync(value);
        }
    }
}
