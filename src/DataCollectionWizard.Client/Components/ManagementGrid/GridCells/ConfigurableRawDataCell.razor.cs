using System.Linq.Expressions;
using DataCollectionWizard.Client.Components.ManagementGrid.Models;
using DataCollectionWizard.Client.Models;
using DataCollectionWizard.Internal.Services.CloudDataflowGenerators;
using Microsoft.AspNetCore.Components;
using ViciOne.DeviceTree.Contracts;

namespace DataCollectionWizard.Client.Components.ManagementGrid.GridCells;

public sealed partial class ConfigurableRawDataCell : ComponentBase
{
    private static readonly Expression<Func<ComboBoxOption<int>, string>> s_intTextSelector = e => e.Text;
    private static readonly Expression<Func<ComboBoxOption<int>, int>> s_intValueSelector = e => e.Value;
    // Which values exist lives in RawDataOptions, shared with the bulk panel; this only wraps them for the ComboBox.
    private static readonly ComboBoxOption<int>[] s_durations =
        [.. RawDataOptions.Durations.Select(duration => new ComboBoxOption<int> { Text = RawDataOptions.DurationToString(duration), Value = duration, })];

    private static readonly ComboBoxOption<int>[] s_frequencies =
        [.. RawDataOptions.Frequencies.Select(frequency => new ComboBoxOption<int> { Text = RawDataOptions.FrequencyToString(frequency), Value = frequency, })];
    private RawDataSettings? _cachedConfig;

    [Parameter]
    public IDeviceTreeConfigurableRawDataNode? ConfigurableRawDataNode { get; set; }

    [Parameter]
    public PublishTargetInfo? Configuration { get; set; }

    [Parameter]
    public int MaxTimesADay { get; set; } = 12;

    [Parameter]
    public EventCallback OnDeviceTreeChanged { get; set; }

    private RawDataSettings Config
        => _cachedConfig ??= ConfigurableRawDataNode!.RawDataConfigurations[Configuration!.Connection.Id];

    protected override void OnParametersSet()
        => _cachedConfig = null;

    private void DurationChanged(int duration)
    {
        Config.Duration = duration;
        OnDeviceTreeChanged.InvokeAsync();
    }

    private void FrequencyChanged(int frequency)
    {
        Config.Frequency = frequency;
        OnDeviceTreeChanged.InvokeAsync();
    }

    private int GetSelectedDuration()
        => Config.Duration;

    private int GetSelectedFrequency()
        => Config.Frequency;

    private bool IsSupportedConnection()
        => Configuration?.IsSupportedForConfiguration(ConfigurableRawDataNode) ?? false;
}
