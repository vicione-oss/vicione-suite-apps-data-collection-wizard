using Microsoft.AspNetCore.Components;
using ViciOne.Ui.Blazor.Components.LoadingSpinner.Models;

namespace DataCollectionWizard.Client.Components.DeviceScan;

/// <summary>
/// The result list of a device scan: a spinner while the scan runs, the scan's messages if it failed, and
/// otherwise the found devices with a checkbox each. What a device looks like and how it is matched against the
/// filter is left to the owner, so the same list serves every kind of scanner.
/// </summary>
/// <typeparam name="TDevice">The kind of device the scan returns.</typeparam>
public sealed partial class DeviceScanList<TDevice> : ComponentBase
{
    /// <summary>
    /// The devices the scan found, or <see langword="null"/> while it is still running.
    /// </summary>
    [Parameter]
    public IReadOnlyList<TDevice>? Devices { get; set; }

    /// <summary>
    /// What the scan reported instead of a result. Anything in here is shown in place of the devices.
    /// </summary>
    [Parameter]
    public IReadOnlyList<string> Errors { get; set; } = [];

    /// <summary>
    /// The text the devices are filtered by. Empty shows all of them.
    /// </summary>
    [Parameter]
    public string Filter { get; set; } = string.Empty;

    /// <summary>
    /// Decides whether a device survives the current filter.
    /// </summary>
    [Parameter, EditorRequired]
    public Func<TDevice, string, bool> MatchesFilter { get; set; } = (_, _) => true;

    /// <summary>
    /// Decides whether a device is currently ticked.
    /// </summary>
    [Parameter, EditorRequired]
    public Func<TDevice, bool> IsSelected { get; set; } = _ => false;

    /// <summary>
    /// Raised when the user ticks or unticks a device.
    /// </summary>
    [Parameter]
    public EventCallback<(bool Selected, TDevice Device)> SelectionChanged { get; set; }

    /// <summary>
    /// How one found device is rendered next to its checkbox.
    /// </summary>
    [Parameter, EditorRequired]
    public RenderFragment<TDevice> DeviceTemplate { get; set; } = default!;

    /// <summary>
    /// The messages the spinner cycles through while the scan runs.
    /// </summary>
    [Parameter]
    public TimedMessage[] SpinnerMessages { get; set; } = [];

    /// <summary>
    /// Shown when the scan itself failed.
    /// </summary>
    [Parameter, EditorRequired]
    public string ScanErrorText { get; set; } = string.Empty;

    /// <summary>
    /// Shown when the scan succeeded but found nothing.
    /// </summary>
    [Parameter, EditorRequired]
    public string NoResultText { get; set; } = string.Empty;

    /// <summary>
    /// Shown when the scan found devices but none of them match the filter.
    /// </summary>
    [Parameter, EditorRequired]
    public string FilterNoResultText { get; set; } = string.Empty;

    private IEnumerable<TDevice> FilterDevices()
        => string.IsNullOrWhiteSpace(Filter)
            ? Devices ?? []
            : (Devices ?? []).Where(d => MatchesFilter(d, Filter));

    private Task OnSelectionChanged(bool selected, TDevice device)
        => SelectionChanged.InvokeAsync((selected, device));
}
