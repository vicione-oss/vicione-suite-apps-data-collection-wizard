using DataCollectionWizard.Client.Components.ManagementGrid.Services;
using DataCollectionWizard.Internal.Services.CloudDataflowGenerators;
using Microsoft.AspNetCore.Components;
using ViciOne.Driver.IoTCore.Contracts.DeviceTree;

namespace DataCollectionWizard.Client.Components.ManagementGrid.GridCells;

public sealed partial class RawDataCell : ComponentBase
{
    private IEnumerable<EventTriggerConfiguration>? _triggers;

    [Parameter]
    public PublishTargetInfo? Configuration { get; set; }

    [Parameter]
    public bool IsExpanded { get; set; }

    [Parameter]
    public int MaxTimesADay { get; set; } = 12;

    [Parameter]
    public EventCallback OnDeviceTreeChanged { get; set; }

    [Parameter]
    public IDeviceTreeEventTriggerDataNode? RawDataNode { get; set; }

    [CascadingParameter]
    private ManagementGridService Service { get; set; } = default!;

    private void DelayChanged(int delay, EventTrigger eventTrigger)
    {
        eventTrigger.Delay = delay;
        OnDeviceTreeChanged.InvokeAsync();
    }

    private void EnabledChanged(bool enabled, EventTrigger eventTrigger)
    {
        eventTrigger.Enabled = enabled;

        Service.InvokeDataPointEnabledChanged(enabled);
        OnDeviceTreeChanged.InvokeAsync();
    }

    private void ErrorChanged(bool error, EventTrigger eventTrigger)
    {
        eventTrigger.OnDamage = error;
        OnDeviceTreeChanged.InvokeAsync();
    }

    public static IEnumerable<EventTriggerConfiguration> FilterTriggerConfigurations(IEnumerable<EventTriggerConfiguration> eventTriggerConfigurations, bool isExpanded)
        => isExpanded
            ? eventTriggerConfigurations
            : [.. eventTriggerConfigurations.Where(e => e.IsSensorConfigured || e.Triggers.Any(t => t.Enabled))];

    private bool IsSupportedConnection()
        => Configuration?.IsSupportedForConfiguration(RawDataNode) ?? false;

    public override Task SetParametersAsync(ParameterView parameters)
    {
        if (parameters.TryGetValue<IDeviceTreeEventTriggerDataNode>(nameof(RawDataNode), out var rawDataNode))
            _triggers = FilterTriggerConfigurations(rawDataNode.EventTriggerConfigurations, IsExpanded);

        if (parameters.TryGetValue<bool>(nameof(IsExpanded), out var isExpanded) && isExpanded != IsExpanded)
            _triggers = FilterTriggerConfigurations(rawDataNode!.EventTriggerConfigurations, isExpanded);

        return base.SetParametersAsync(parameters);
    }

    private void WarningChanged(bool warning, EventTrigger eventTrigger)
    {
        eventTrigger.OnWarning = warning;
        OnDeviceTreeChanged.InvokeAsync();
    }
}
