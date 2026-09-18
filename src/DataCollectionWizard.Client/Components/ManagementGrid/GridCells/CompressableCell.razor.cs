using System.Linq.Expressions;
using DataCollectionWizard.Client.Components.ManagementGrid.Models;
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
    // Which options exist per connection kind lives in AggregationOptions, shared with the bulk panel; this only
    // wraps them for the ComboBox.
    private static readonly Dictionary<ConnectionKind, ComboBoxOption<AggregationInterval>[]> s_aggregationIntervals =
        Enum.GetValues<ConnectionKind>().ToDictionary(
            kind => kind,
            kind => AggregationOptions.IntervalsFor(kind)
                .Select(interval => new ComboBoxOption<AggregationInterval> { Text = interval.AggregationIntervalToString(), Value = interval, })
                .ToArray());

    private static readonly Dictionary<ConnectionKind, ComboBoxOption<AggregationFunction>[]> s_aggregationFunctions =
        Enum.GetValues<ConnectionKind>().ToDictionary(
            kind => kind,
            kind => AggregationOptions.FunctionsFor(kind)
                .Select(function => new ComboBoxOption<AggregationFunction> { Text = function.AggregationFunctionToString(), Value = function, })
                .ToArray());
    private static readonly Expression<Func<ComboBoxOption<AggregationInterval>, string>> s_aggregationIntervalTextSelector = e => e.Text;
    private static readonly Expression<Func<ComboBoxOption<AggregationInterval>, AggregationInterval>> s_aggregationIntervalValueSelector = e => e.Value;
    private static readonly Expression<Func<ComboBoxOption<AggregationFunction>, string>> s_aggregationFunctionTextSelector = e => e.Text;
    private static readonly Expression<Func<ComboBoxOption<AggregationFunction>, AggregationFunction>> s_aggregationFunctionValueSelector = e => e.Value;
    private CompressorConfiguration? _cachedConfig;
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

    private ComboBoxOption<AggregationInterval>[] AggregationIntervals
        => s_aggregationIntervals.TryGetValue(Configuration.Kind, out var intervals) ? intervals : [];

    private ComboBoxOption<AggregationFunction>[] AggregationFunctions
        => s_aggregationFunctions.TryGetValue(Configuration.Kind, out var functions) ? functions : [];

    protected override void OnParametersSet()
    {
        if (_previousRenderEpoch == RenderEpoch &&
            ReferenceEquals(_previousDataNode, CompressableDataNode) &&
            ReferenceEquals(_previousConfiguration, Configuration))
        {
            return;
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
