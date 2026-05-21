using System.Linq.Expressions;
using DataCollectionWizard.Client.Components.ManagementGrid.Services;
using DataCollectionWizard.Client.Extensions;
using DataCollectionWizard.Client.Models;
using DataCollectionWizard.Internal.Contracts;
using DataCollectionWizard.Internal.Extensions;
using DataCollectionWizard.Internal.Services.CloudDataflowGenerators;
using Microsoft.AspNetCore.Components;
using ViciOne.Driver.IoTCore.Contracts.DeviceTree;

namespace DataCollectionWizard.Client.Components.ManagementGrid.GridCells;

public sealed partial class CompressableCell : ComponentBase
{
    private static readonly ComboBoxOption<PoolingGrid>[] s_poolingGridsAnna =
    [
        new() { Text = PoolingGrid.OnChange.PoolingGridToString(), Value = PoolingGrid.OnChange, },
        new() { Text = PoolingGrid.SecondsOne.PoolingGridToString(), Value = PoolingGrid.SecondsOne, },
        new() { Text = PoolingGrid.SecondsFive.PoolingGridToString(), Value = PoolingGrid.SecondsFive, },
        new() { Text = PoolingGrid.SecondsTen.PoolingGridToString(), Value = PoolingGrid.SecondsTen, },
        new() { Text = PoolingGrid.SecondsThirty.PoolingGridToString(), Value = PoolingGrid.SecondsThirty, },
        new() { Text = PoolingGrid.MinutesOne.PoolingGridToString(), Value = PoolingGrid.MinutesOne, },
        new() { Text = PoolingGrid.MinutesTwo.PoolingGridToString(), Value = PoolingGrid.MinutesTwo, },
        new() { Text = PoolingGrid.MinutesFive.PoolingGridToString(), Value = PoolingGrid.MinutesFive, },
        new() { Text = PoolingGrid.MinutesTen.PoolingGridToString(), Value = PoolingGrid.MinutesTen, },
        new() { Text = PoolingGrid.MinutesThirty.PoolingGridToString(), Value = PoolingGrid.MinutesThirty, },
        new() { Text = PoolingGrid.HoursOne.PoolingGridToString(), Value = PoolingGrid.HoursOne, },
    ];
    private static readonly ComboBoxOption<PoolingGrid>[] s_poolingGridsMoneo =
    [
        new() { Text = PoolingGrid.SecondsOne.PoolingGridToString(), Value = PoolingGrid.SecondsOne, },
        new() { Text = PoolingGrid.SecondsTen.PoolingGridToString(), Value = PoolingGrid.SecondsTen, },
        new() { Text = PoolingGrid.MinutesOne.PoolingGridToString(), Value = PoolingGrid.MinutesOne, },
    ];
    private static readonly ComboBoxOption<PoolingMode>[] s_poolingModesAnna =
    [
        new() { Text = PoolingMode.MinMaxAvg.PoolingModeToString(), Value = PoolingMode.MinMaxAvg, },
        new() { Text = PoolingMode.Avg.PoolingModeToString(), Value = PoolingMode.Avg, },
        new() { Text = PoolingMode.Min.PoolingModeToString(), Value = PoolingMode.Min, },
        new() { Text = PoolingMode.Max.PoolingModeToString(), Value = PoolingMode.Max, },
    ];
    private static readonly ComboBoxOption<PoolingMode>[] s_poolingModesMoneo =
    [
        new() { Text = PoolingMode.Last.PoolingModeToString(), Value = PoolingMode.Last, },
        new() { Text = PoolingMode.Avg.PoolingModeToString(), Value = PoolingMode.Avg, },
        new() { Text = PoolingMode.Min.PoolingModeToString(), Value = PoolingMode.Min, },
        new() { Text = PoolingMode.Max.PoolingModeToString(), Value = PoolingMode.Max, },
    ];
    private static readonly Expression<Func<ComboBoxOption<PoolingGrid>, string>> s_poolingGridTextSelector = e => e.Text;
    private static readonly Expression<Func<ComboBoxOption<PoolingGrid>, PoolingGrid>> s_poolingGridValueSelector = e => e.Value;
    private static readonly Expression<Func<ComboBoxOption<PoolingMode>, string>> s_poolingModeTextSelector = e => e.Text;
    private static readonly Expression<Func<ComboBoxOption<PoolingMode>, PoolingMode>> s_poolingModeValueSelector = e => e.Value;
    private CompressorConfiguration? _cachedConfig;
    private bool _shouldRender = true;
    private IDeviceTreeCompressableDataNode? _previousDataNode;
    private PublishTargetInfo? _previousConfiguration;

    [CascadingParameter]
    private ManagementGridService Service { get; set; } = default!;

    [Parameter, EditorRequired]
    public IDeviceTreeCompressableDataNode CompressableDataNode { get; set; } = default!;

    [Parameter, EditorRequired]
    public PublishTargetInfo Configuration { get; set; } = default!;

    [Parameter]
    public EventCallback OnDeviceTreeChanged { get; set; }

    private CompressorConfiguration Config
        => _cachedConfig ??= CompressableDataNode.CompressorConfigurations
            .Single(cc => cc.DataGroupIdentifier == Configuration.Connection.Id);

    private ComboBoxOption<PoolingGrid>[] PoolingGrids
        => Configuration.Kind switch
        {
            ConnectionKind.Anna => s_poolingGridsAnna,
            ConnectionKind.Moneo => s_poolingGridsMoneo,
            _ => [],
        };

    private ComboBoxOption<PoolingMode>[] PoolingModes
        => Configuration.Kind switch
        {
            ConnectionKind.Anna => s_poolingModesAnna,
            ConnectionKind.Moneo => s_poolingModesMoneo,
            _ => [],
        };

    protected override void OnParametersSet()
    {
        if (ReferenceEquals(_previousDataNode, CompressableDataNode)
            && ReferenceEquals(_previousConfiguration, Configuration))
            return;

        _previousDataNode = CompressableDataNode;
        _previousConfiguration = Configuration;
        _cachedConfig = null;
        _shouldRender = true;
    }

    protected override bool ShouldRender()
    {
        if (!_shouldRender)
            return false;

        _shouldRender = false;
        return true;
    }

    private PoolingGrid GetSelectedPoolingGrid()
        => Config.CompressionTime.ToPoolingGrid();

    private PoolingMode GetSelectedPoolingMode()
        => Config.PoolingMode;

    public bool IsOnChange()
        => Config.CompressionTime == -1;

    private bool IsPoolingEnabled()
        => Config.Enabled;

    private bool IsSupportedConnection()
    {
        if (CompressableDataNode is null)
        {
            return false;
        }

        return Configuration?.IsSupportedForConfiguration(CompressableDataNode) ?? false;
    }

    private void PoolingEnabledChanged(bool isEnabled)
    {
        Config.Enabled = isEnabled;
        _shouldRender = true;

        Service.InvokeDataPointEnabledChanged(isEnabled);
        OnDeviceTreeChanged.InvokeAsync();
    }

    private void PoolingGridChanged(PoolingGrid poolingGrid)
    {
        Config.CompressionTime = (int)poolingGrid;
        _shouldRender = true;

        OnDeviceTreeChanged.InvokeAsync();
    }

    private void PoolingModeChanged(PoolingMode poolingMode)
    {
        Config.PoolingMode = poolingMode;
        _shouldRender = true;

        OnDeviceTreeChanged.InvokeAsync();
    }
}
