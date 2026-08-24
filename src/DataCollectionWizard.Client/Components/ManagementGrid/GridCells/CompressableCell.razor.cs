using System.Linq.Expressions;
using DataCollectionWizard.Client.Components.ManagementGrid.Services;
using DataCollectionWizard.Client.Extensions;
using DataCollectionWizard.Client.Models;
using DataCollectionWizard.Internal.Contracts;
using DataCollectionWizard.Internal.Extensions;
using DataCollectionWizard.Internal.Services.CloudDataflowGenerators;
using Microsoft.AspNetCore.Components;
using ViciOne.DeviceTree.Contracts;

namespace DataCollectionWizard.Client.Components.ManagementGrid.GridCells;

public sealed partial class CompressableCell : ComponentBase
{
    private static readonly ComboBoxOption<AggregationInterval>[] s_aggregationIntervalsAnna =
    [
        new() { Text = AggregationInterval.OnChange.AggregationIntervalToString(), Value = AggregationInterval.OnChange, },
        new() { Text = AggregationInterval.SecondsOne.AggregationIntervalToString(), Value = AggregationInterval.SecondsOne, },
        new() { Text = AggregationInterval.SecondsFive.AggregationIntervalToString(), Value = AggregationInterval.SecondsFive, },
        new() { Text = AggregationInterval.SecondsTen.AggregationIntervalToString(), Value = AggregationInterval.SecondsTen, },
        new() { Text = AggregationInterval.SecondsThirty.AggregationIntervalToString(), Value = AggregationInterval.SecondsThirty, },
        new() { Text = AggregationInterval.MinutesOne.AggregationIntervalToString(), Value = AggregationInterval.MinutesOne, },
        new() { Text = AggregationInterval.MinutesTwo.AggregationIntervalToString(), Value = AggregationInterval.MinutesTwo, },
        new() { Text = AggregationInterval.MinutesFive.AggregationIntervalToString(), Value = AggregationInterval.MinutesFive, },
        new() { Text = AggregationInterval.MinutesTen.AggregationIntervalToString(), Value = AggregationInterval.MinutesTen, },
        new() { Text = AggregationInterval.MinutesThirty.AggregationIntervalToString(), Value = AggregationInterval.MinutesThirty, },
        new() { Text = AggregationInterval.HoursOne.AggregationIntervalToString(), Value = AggregationInterval.HoursOne, },
    ];
    private static readonly ComboBoxOption<AggregationInterval>[] s_aggregationIntervalsMoneo =
    [
        new() { Text = AggregationInterval.SecondsOne.AggregationIntervalToString(), Value = AggregationInterval.SecondsOne, },
        new() { Text = AggregationInterval.SecondsTen.AggregationIntervalToString(), Value = AggregationInterval.SecondsTen, },
        new() { Text = AggregationInterval.MinutesOne.AggregationIntervalToString(), Value = AggregationInterval.MinutesOne, },
    ];
    private static readonly ComboBoxOption<AggregationFunction>[] s_aggregationFunctionsAnna =
    [
        new() { Text = AggregationFunction.MinMaxAvg.AggregationFunctionToString(), Value = AggregationFunction.MinMaxAvg, },
        new() { Text = AggregationFunction.Avg.AggregationFunctionToString(), Value = AggregationFunction.Avg, },
        new() { Text = AggregationFunction.Min.AggregationFunctionToString(), Value = AggregationFunction.Min, },
        new() { Text = AggregationFunction.Max.AggregationFunctionToString(), Value = AggregationFunction.Max, },
    ];
    private static readonly ComboBoxOption<AggregationFunction>[] s_aggregationFunctionsMoneo =
    [
        new() { Text = AggregationFunction.Last.AggregationFunctionToString(), Value = AggregationFunction.Last, },
        new() { Text = AggregationFunction.Avg.AggregationFunctionToString(), Value = AggregationFunction.Avg, },
        new() { Text = AggregationFunction.Min.AggregationFunctionToString(), Value = AggregationFunction.Min, },
        new() { Text = AggregationFunction.Max.AggregationFunctionToString(), Value = AggregationFunction.Max, },
    ];
    private static readonly Expression<Func<ComboBoxOption<AggregationInterval>, string>> s_aggregationIntervalTextSelector = e => e.Text;
    private static readonly Expression<Func<ComboBoxOption<AggregationInterval>, AggregationInterval>> s_aggregationIntervalValueSelector = e => e.Value;
    private static readonly Expression<Func<ComboBoxOption<AggregationFunction>, string>> s_aggregationFunctionTextSelector = e => e.Text;
    private static readonly Expression<Func<ComboBoxOption<AggregationFunction>, AggregationFunction>> s_aggregationFunctionValueSelector = e => e.Value;
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

    private ComboBoxOption<AggregationInterval>[] AggregationIntervals
        => Configuration.Kind switch
        {
            ConnectionKind.Anna => s_aggregationIntervalsAnna,
            ConnectionKind.Moneo => s_aggregationIntervalsMoneo,
            _ => [],
        };

    private ComboBoxOption<AggregationFunction>[] AggregationFunctions
        => Configuration.Kind switch
        {
            ConnectionKind.Anna => s_aggregationFunctionsAnna,
            ConnectionKind.Moneo => s_aggregationFunctionsMoneo,
            _ => [],
        };

    protected override void OnParametersSet()
    {
        if (ReferenceEquals(_previousDataNode, CompressableDataNode) &&
            ReferenceEquals(_previousConfiguration, Configuration))
        {
            return;
        }

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

    private AggregationInterval GetSelectedAggregationInterval()
        => Config.CompressionTime.ToAggregationInterval();

    private AggregationFunction GetSelectedAggregationFunction()
        => Config.Aggregation;

    public bool IsOnChange()
        => Config.CompressionTime == -1;

    private bool IsCompressionEnabled()
        => Config.Enabled;

    private bool IsSupportedConnection()
        => Configuration?.IsSupportedForConfiguration(CompressableDataNode) ?? false;

    private void CompressionEnabledChanged(bool isEnabled)
    {
        Config.Enabled = isEnabled;
        _shouldRender = true;

        Service.InvokeDataPointEnabledChanged(isEnabled);
        OnDeviceTreeChanged.InvokeAsync();
    }

    private void AggregationIntervalChanged(AggregationInterval aggregationInterval)
    {
        Config.CompressionTime = (int)aggregationInterval;
        _shouldRender = true;

        Service.InvokeConfigChanged();
        OnDeviceTreeChanged.InvokeAsync();
    }

    private void AggregationFunctionChanged(AggregationFunction aggregationFunction)
    {
        Config.Aggregation = aggregationFunction;
        _shouldRender = true;

        Service.InvokeConfigChanged();
        OnDeviceTreeChanged.InvokeAsync();
    }
}
