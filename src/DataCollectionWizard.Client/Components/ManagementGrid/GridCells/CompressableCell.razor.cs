using DataCollectionWizard.Client.Components.ManagementGrid.Services;
using DataCollectionWizard.Client.Extensions;
using DataCollectionWizard.Client.Models;
using DataCollectionWizard.Internal.Contracts;
using DataCollectionWizard.Internal.Extensions;
using DataCollectionWizard.Internal.Services.CloudDataflowGenerators;
using Microsoft.AspNetCore.Components;
using Sdk.Connections.Contracts;
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

    [CascadingParameter]
    private ManagementGridService Service { get; set; } = default!;

    [Parameter, EditorRequired]
    public IDeviceTreeCompressableDataNode CompressableDataNode { get; set; } = default!;

    [Parameter, EditorRequired]
    public Connection Configuration { get; set; } = default!;

    [Parameter]
    public EventCallback OnDeviceTreeChanged { get; set; }

    private ComboBoxOption<PoolingGrid>[] PoolingGrids
    {
        get
        {
            if (Configuration is not null && new AnnaCloudFilter().GetCloudConnections([Configuration]).Any())
                return s_poolingGridsAnna;

            if (Configuration is not null && new MoneoCloudFilter().GetCloudConnections([Configuration]).Any())
                return s_poolingGridsMoneo;

            return [];
        }
    }

    private ComboBoxOption<PoolingMode>[] PoolingModes
    {
        get
        {
            if (Configuration is not null && new AnnaCloudFilter().GetCloudConnections([Configuration]).Any())
                return s_poolingModesAnna;

            if (Configuration is not null && new MoneoCloudFilter().GetCloudConnections([Configuration]).Any())
                return s_poolingModesMoneo;

            return [];
        }
    }

    private PoolingGrid GetSelectedPoolingGrid()
        => CompressableDataNode.CompressorConfigurations
            .Single(cc => cc.DataGroupIdentifier == Configuration.Id)
            .CompressionTime
            .ToPoolingGrid();

    private PoolingMode GetSelectedPoolingMode()
        => CompressableDataNode.CompressorConfigurations
            .Single(cc => cc.DataGroupIdentifier == Configuration.Id)
            .PoolingMode;

    public bool IsOnChange()
        => CompressableDataNode.CompressorConfigurations
            .Single(cc => cc.DataGroupIdentifier == Configuration.Id)
            .CompressionTime == -1;

    private bool IsPoolingEnabled()
        => CompressableDataNode.CompressorConfigurations
            .Single(cc => cc.DataGroupIdentifier == Configuration.Id)
            .Enabled;

    private bool IsSupportedConnection()
        => Configuration is not null && new AnnaCloudFilter().GetCloudConnections([Configuration]).Any();

    private void PoolingEnabledChanged(bool isEnabled)
    {
        CompressableDataNode.CompressorConfigurations
            .Single(cc => cc.DataGroupIdentifier == Configuration.Id)
            .Enabled = isEnabled;

        Service.InvokeDataPointEnabledChanged(isEnabled);
        OnDeviceTreeChanged.InvokeAsync();
    }

    private void PoolingGridChanged(PoolingGrid poolingGrid)
    {
        CompressableDataNode.CompressorConfigurations
            .Single(cc => cc.DataGroupIdentifier == Configuration.Id)
            .CompressionTime = (int)poolingGrid;

        OnDeviceTreeChanged.InvokeAsync();
    }

    private void PoolingModeChanged(PoolingMode poolingMode)
    {
        CompressableDataNode.CompressorConfigurations
            .Single(cc => cc.DataGroupIdentifier == Configuration.Id)
            .PoolingMode = poolingMode;

        OnDeviceTreeChanged.InvokeAsync();
    }
}
