using DataCollectionWizard.Client.Models;
using DataCollectionWizard.Internal.Services.CloudDataflowGenerators;
using Microsoft.AspNetCore.Components;
using ViciOne.Driver.IoTCore.Contracts.DeviceTree;

namespace DataCollectionWizard.Client.Components.ManagementGrid.GridCells;

public sealed partial class ConfigurableRawDataCell : ComponentBase
{
    private readonly ComboBoxOption<int>[] _durations =
    [
        new() { Text = "1s", Value = 1000, },
        new() { Text = "2s", Value = 2000, },
        new() { Text = "3s", Value = 3000, },
        new() { Text = "4s", Value = 4000, },
        new() { Text = "5s", Value = 5000, },
        new() { Text = "6s", Value = 6000, },
        new() { Text = "7s", Value = 7000, },
        new() { Text = "8s", Value = 8000, },
        new() { Text = "9s", Value = 9000, },
        new() { Text = "10s", Value = 10000, },
    ];
    private readonly ComboBoxOption<int>[] _frequencies =
    [
        new() { Text = $"50k Sample/s", Value = 50000, },
        new() { Text = $"100k Sample/s", Value = 100000, },
    ];

    [Parameter]
    public IDeviceTreeConfigurableRawDataNode? ConfigurableRawDataNode { get; set; }

    [Parameter]
    public PublishTargetInfo? Configuration { get; set; }

    [Parameter]
    public int MaxTimesADay { get; set; } = 12;

    [Parameter]
    public EventCallback OnDeviceTreeChanged { get; set; }

    private void DurationChanged(int duration)
    {
        ConfigurableRawDataNode!.RawDataConfigurations[Configuration!.Connection.Id].Duration = duration;
        OnDeviceTreeChanged.InvokeAsync();
    }

    private void FrequencyChanged(int frequency)
    {
        ConfigurableRawDataNode!.RawDataConfigurations[Configuration!.Connection.Id].Frequency = frequency;
        OnDeviceTreeChanged.InvokeAsync();
    }

    private int GetSelectedDuration()
        => ConfigurableRawDataNode!.RawDataConfigurations[Configuration!.Connection.Id].Duration;

    private int GetSelectedFrequency()
        => ConfigurableRawDataNode!.RawDataConfigurations[Configuration!.Connection.Id].Frequency;

    private bool IsSupportedConnection()
        => Configuration?.IsSupported ?? false;
}
