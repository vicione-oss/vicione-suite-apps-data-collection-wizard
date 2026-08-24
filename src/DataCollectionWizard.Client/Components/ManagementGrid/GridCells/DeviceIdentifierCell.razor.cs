using DataCollectionWizard.Client.Components.ManagementGrid.Models;
using DataCollectionWizard.Internal.Services.CloudDataflowGenerators;
using Microsoft.AspNetCore.Components;
using ViciOne.DeviceTree.Contracts;

namespace DataCollectionWizard.Client.Components.ManagementGrid.GridCells;

public sealed partial class DeviceIdentifierCell : ComponentBase
{
    private readonly ExternalParameters _externalParameters = new();
    private IEnumerable<EventTriggerConfiguration> _visibleTriggers = null!;

#pragma warning disable CA2227 // Collection properties should be read only - required for Blazor parameter binding
    [Parameter, EditorRequired]
    public Dictionary<string, IDeviceTreeBase> AllNodes { get; set; } = null!;
#pragma warning restore CA2227 // Collection properties should be read only - required for Blazor parameter binding

    /// <summary>
    /// Where this value sits in the tree, shown as a second line under its name, or <see langword="null"/> to
    /// show none.
    /// </summary>
    /// <remarks>
    /// The grid passes this only for rows whose position is not already evident - it stays empty while the path
    /// column is visible, and for rows sitting under a group header that names their parent.
    /// </remarks>
    [Parameter]
    public string? Context { get; set; }

    [Parameter]
    public EventCallback<ManagementGridRowModel> ExpandedChanged { get; set; }

    [Parameter, EditorRequired]
    public ManagementGridRowModel Model { get; set; } = default!;

    [Parameter, EditorRequired]
    public IReadOnlyList<PublishTargetInfo> PublishTargetInfos { get; set; } = [];

    public static IEnumerable<EventTriggerConfiguration> FilterTriggerConfigurations(List<EventTriggerConfiguration> eventTriggerConfigurations, bool isExpanded)
        => isExpanded
            ? eventTriggerConfigurations
            : [.. eventTriggerConfigurations.Where(e => e.IsSensorConfigured || e.Triggers.Any(t => t.Enabled))];

    protected override void OnParametersSet()
    {
        if (Model != _externalParameters.Model)
        {
            _externalParameters.Model = Model;
            _visibleTriggers = null!;
        }

        if (_visibleTriggers is null && Model.DataNode is IDeviceTreeEventTriggerDataNode triggerNode)
            _visibleTriggers = FilterTriggerConfigurations(triggerNode.EventTriggerConfigurations, Model.IsExpanded);
    }

    private async Task SetIsExpandedAsync(bool value, IDeviceTreeEventTriggerDataNode triggerNode)
    {
        Model.IsExpanded = value;
        _visibleTriggers = FilterTriggerConfigurations(triggerNode.EventTriggerConfigurations, Model.IsExpanded);
        await ExpandedChanged.InvokeAsync(Model);
    }

    private record ExternalParameters
    {
        public ManagementGridRowModel Model { get; set; } = default!;
    }
}
