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
    private static readonly Expression<Func<ComboBoxOption<AggregationInterval>, string>> s_aggregationIntervalTextSelector = e => e.Text;
    private static readonly Expression<Func<ComboBoxOption<AggregationInterval>, AggregationInterval>> s_aggregationIntervalValueSelector = e => e.Value;
    private static readonly Expression<Func<ComboBoxOption<AggregationFunction>, string>> s_aggregationFunctionTextSelector = e => e.Text;
    private static readonly Expression<Func<ComboBoxOption<AggregationFunction>, AggregationFunction>> s_aggregationFunctionValueSelector = e => e.Value;
    private CompressorConfiguration? _cachedConfig;
    private ComboBoxOption<AggregationInterval>[]? _aggregationIntervals;
    private ComboBoxOption<AggregationFunction>[]? _aggregationFunctions;
    private bool _shouldRender = true;
    private IDeviceTreeCompressableDataNode? _previousDataNode;
    private PublishTargetInfo? _previousConfiguration;
    private int _previousRenderEpoch;

    [CascadingParameter]
    private ManagementGridService Service { get; set; } = default!;

    [Parameter, EditorRequired]
    public IDeviceTreeCompressableDataNode CompressableDataNode { get; set; } = default!;

    [Parameter, EditorRequired]
    public PublishTargetInfo Configuration { get; set; } = default!;

    [Parameter]
    public EventCallback OnDeviceTreeChanged { get; set; }

    /// <summary>
    /// Bumped by the grid whenever something outside this cell wrote its configuration.
    /// </summary>
    /// <remarks>
    /// The cell renders once per parameter change and then blocks, and a bulk change writes the configuration
    /// object in place - so nothing it can see has changed and it would keep showing the old toggle and combo.
    /// A changing number is enough to let one render through. The grid used to re-key every row for this, which
    /// tore down and rebuilt every visible row's components for the sake of the few fields that actually moved.
    /// </remarks>
    [Parameter]
    public int RenderEpoch { get; set; }

    private CompressorConfiguration Config
        => _cachedConfig ??= CompressableDataNode.CompressorConfigurations
            .Single(cc => cc.DataGroupIdentifier == Configuration.Connection.Id);

    // Which options this row's cloud offers comes straight from its ICloudFilter, via Configuration
    // (PublishTargetInfo) - this only wraps them for the ComboBox. Cached per Configuration so each render hands
    // the ComboBox the same Items instance.
    private ComboBoxOption<AggregationInterval>[] AggregationIntervals
        => _aggregationIntervals ??= [.. Configuration.SupportedAggregationIntervals
            .Select(interval => new ComboBoxOption<AggregationInterval> { Text = interval.AggregationIntervalToString(), Value = interval, })];

    private ComboBoxOption<AggregationFunction>[] AggregationFunctions
        => _aggregationFunctions ??= [.. Configuration.SupportedAggregationFunctions
            .Select(function => new ComboBoxOption<AggregationFunction> { Text = function.AggregationFunctionToString(), Value = function, })];

    protected override void OnParametersSet()
    {
        if (_previousRenderEpoch == RenderEpoch &&
            ReferenceEquals(_previousDataNode, CompressableDataNode) &&
            ReferenceEquals(_previousConfiguration, Configuration))
        {
            return;
        }

        if (!ReferenceEquals(_previousConfiguration, Configuration))
        {
            _aggregationIntervals = null;
            _aggregationFunctions = null;
        }

        _previousRenderEpoch = RenderEpoch;
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
