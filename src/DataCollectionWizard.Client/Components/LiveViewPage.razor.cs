using System.Collections.Concurrent;
using System.Globalization;
using System.Text.Json;
using ClusterManagement.Public.DataflowEvents;
using ClusterManagement.Public.Services;
using DataCollectionWizard.Client.Components.LiveGrid.Models;
using DataCollectionWizard.Client.Components.LiveGrid.Services;
using DataCollectionWizard.Client.Extensions;
using DataCollectionWizard.Client.Services;
using DataCollectionWizard.Internal.Contracts;
using DataCollectionWizard.Internal.Services;
using DataCollectionWizard.Public.Extensions;
using Microsoft.AspNetCore.Components;
using Sdk.Client.Modules;
using ViciOne.DeviceTree.Contracts;
using ViciOne.DeviceTree.Contracts.Extensions;
using ViciOne.Ui.Blazor.Components.Dialog.Components;
using ViciOne.Ui.Blazor.Components.LoadingSpinner.Factories;
using ViciOne.Ui.Blazor.Components.LoadingSpinner.Models;
using ViciOne.Ui.Localization.Resources;

namespace DataCollectionWizard.Client.Components;

public sealed partial class LiveViewPage : ModulePageBase<DataCollectionWizardClientModule>
{
    private DeviceTreeAdapter _adapter = default!;
    private CancellationTokenSource _cancelSubscribing = new();
    private bool _displayLoadingSpinner;
    private List<IDeviceTreeLiveDataNode> _gridNodes = [];
    private readonly ConcurrentDictionary<IDeviceTreeDataNode, (IAsyncDisposable?, IAsyncDisposable? Unit)> _handles = [];
    private readonly TimedMessage[] _loadingSpinnerMessages =
    [
        new()
        {
            DisplayDuration = 3,
            Message = Localization.DataCollectionWizardPage.SpinnerMessageSubscribingResultOutput,
        },
        TimedMessageFactory.CreateGap(1),
        new()
        {
            DisplayDuration = 3,
            Message = Localization.DataCollectionWizardPage.SpinnerMessageTriggeringDeviceTreeScan,
        },
        TimedMessageFactory.CreateGap(1),
        new()
        {
            Message = Localization.DataCollectionWizardPage.SpinnerMessageWaitingForScanResult,
        },
    ];
    private Dictionary<IDeviceTreeBase, List<IDeviceTreeBase>>? _nodePaths;
    private Dialog? _refLatestClusterNotRunningDialog;
    private Dialog? _refDataInvalidDialog;
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly LiveGridService _service = new();
    private DeviceTreeRoot _tree = new();

    public bool ShowMessageCommandWithEventCallback { get; set; }
    public bool ShowMessageSimpleCommand { get; set; }

    protected override string PageTitle => Localization.LiveViewPage.Title;

    [Inject] private IDataCollectionWizardService DataCollectionWizardService { get; set; } = null!;
    [Inject] private IEventBroker EventBroker { get; set; } = null!;
    [Inject] private IResourceDownloadStateService ResourceDownloadState { get; set; } = null!;

    [Inject(Key = Sdk.Constants.ClientTimeProviderServiceKey)]
    private TimeProvider TimeProvider { get; set; } = default!;

    [Inject] private DeviceTreeNodeIconProvider IconProvider { get; set; } = default!;

    private LiveGridRowModel[] CalculateGridItems(List<IDeviceTreeLiveDataNode> _)
    {
        return [.. _adapter.GetRelevantDataNodes()
            .Where(n => n is not IDeviceTreeHiddenNode && n.DataType.SupportsLiveView)
            .Select(DataNodeToGridModel)
            .Distinct()];

        LiveGridRowModel DataNodeToGridModel(IDeviceTreeDataNode dataNode)
            => new()
            {
                DataNode = dataNode,
                PathToNode = _nodePaths![dataNode],
            };
    }

    protected override async ValueTask DisposeInternal()
    {
        _adapter.SelectionChanged -= OnTreeSelectionChangedAsync;
        _service.FilteredGridItemsChanged -= OnFilterChangedAsync;
        _service.RebrowseRequested -= OnRebrowse;

        _cancelSubscribing.Dispose();

        DataCollectionWizardService.NodesOnline -= NodesOnline;
        DataCollectionWizardService.NodesOffline -= NodesOffline;

        await UnsubscribeAllAsync();

        _semaphore.Dispose();
        _service.Dispose();

        await base.DisposeInternal();
    }

    private static string FormatValue(string? value, CultureInfo culture)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        return double.TryParse(value, CultureInfo.InvariantCulture, out var number)
            ? number.ToString(culture)
            : value;
    }

    private async Task InitLiveView()
    {
        await ResourceDownloadState.WaitForCompletion();
        await DataCollectionWizardService.WaitForCurrentDeployment(new TimeSpan(0, 1, 0));
        await UpdateDeviceTreeAsync();

        if (!await DataCollectionWizardService.IsClusterRunningAsync())
        {
            await InvokeAsync(_refLatestClusterNotRunningDialog!.ShowAsync);
        }
    }

    // Only the affected nodes, not the whole tree. SetDeviceTree ends by raising SelectionChanged, which this
    // page answers by dropping and rebuilding every subscription - on a status change there is nothing to rebuild,
    // since the dataflow is unchanged and the connectors stay in place when a sensor drops out.
    private async Task NodesOffline(string[] nodeIds)
    {
        if (_tree is null)
            return;

        SetNodesStatus(nodeIds, ConnectionStatus.Offline);
        _adapter.UpdateNodeStatuses(nodeIds, expandToOfflineNodes: true);
        await InvokeAsync(StateHasChanged);
    }

    private async Task NodesOnline(string[] nodeIds)
    {
        if (_tree is null)
            return;

        SetNodesStatus(nodeIds, ConnectionStatus.Online);
        _adapter.UpdateNodeStatuses(nodeIds);
        await InvokeAsync(StateHasChanged);
    }

    private async Task OnDataInvalidDialogOkAsync()
        => await _refDataInvalidDialog!.CloseAsync();

    private void OnDataPossiblyInvalid(bool deviceTreeChanged)
        => _ = Task.Run(async () =>
        {
            try
            {
                if (deviceTreeChanged)
                {
                    await InvokeAsync(_refDataInvalidDialog!.ShowAsync);
                    return;
                }

                _displayLoadingSpinner = true;
                await InvokeAsync(StateHasChanged);
                await DataCollectionWizardService.WaitForCurrentDeployment(new TimeSpan(0, 1, 0));

                if (deviceTreeChanged)
                    SetTree(await DataCollectionWizardService.RequestDeviceTreeAsync(), false);

                await UpdateDeviceTreeAsync();
            }
            catch (Exception ex)
            {
                LogAwaitingDeploymentWarning(Logger, ex);
            }
        });

    protected override async Task OnInitializedAsync()
    {
        _adapter = new DeviceTreeAdapter(true, IconProvider);

        _displayLoadingSpinner = true;

        _service.TreeBuilder.SetAdapter(_adapter);

        _service.FilteredGridItemsChanged += OnFilterChangedAsync;
        _service.RebrowseRequested += OnRebrowse;

        SetTree(await DataCollectionWizardService.RequestDeviceTreeAsync(), false);

        _adapter.SelectionChanged += OnTreeSelectionChangedAsync;
        _adapter.Builder.Expansion.ChangeExpansionForLayers(true, 0, 0);

        DataCollectionWizardService.DataPossiblyInvalid += OnDataPossiblyInvalid;
        DataCollectionWizardService.NodesOnline += NodesOnline;
        DataCollectionWizardService.NodesOffline += NodesOffline;

        _ = Task.Run(InitLiveView);

        await base.OnInitializedAsync();
    }

    private async Task OnLatestClusterNotRunningDialogOkAsync()
        => await _refLatestClusterNotRunningDialog!.CloseAsync();

    private async void OnFilterChangedAsync()
    {
        var acquired = false;

        try
        {
            await _semaphore.WaitAsync();

            acquired = true;

            await UnsubscribeAllAsync();
            await SubscribeAllAsync(_cancelSubscribing.Token);

            // Reported once the rebuild has settled - in between there is nothing subscribed, and that gap is
            // not the end of the observation window.
            _service.Session.SetWatching(!_handles.IsEmpty);
        }
        catch (Exception ex)
        {
            LogSubscribeAllError(Logger, ex);
        }
        finally
        {
            if (acquired)
            {
                try { _semaphore.Release(); }
                catch (ObjectDisposedException) { }
            }
        }
    }

    private void OnRebrowse()
    {
        _displayLoadingSpinner = true;
        InvokeAsync(StateHasChanged);
        _ = UpdateDeviceTreeAsync();
    }

    private async void OnTreeSelectionChangedAsync()
    {
        var acquired = false;

        try
        {
            await _semaphore.WaitAsync();

            acquired = true;

            _cancelSubscribing.Cancel();
            _cancelSubscribing.Dispose();
            _cancelSubscribing = new();
            await UnsubscribeAllAsync();
            _gridNodes = [.. _adapter.GetRelevantDataNodes().OfType<IDeviceTreeLiveDataNode>().Where(n => n is not IDeviceTreeHiddenNode)];
            _service.SetGridItems(CalculateGridItems(_gridNodes), false);
            await SubscribeAllAsync(_cancelSubscribing.Token);
            _service.Session.SetWatching(!_handles.IsEmpty);
            _service.RefreshImmediate();
        }
        catch (Exception ex)
        {
            LogSubscribeAllError(Logger, ex);
        }
        finally
        {
            if (acquired)
            {
                try { _semaphore.Release(); }
                catch (ObjectDisposedException) { }
            }
        }
    }

    private static void RemoveNewNodesRecursively(IDeviceTreeBase tree)
    {
        for (var i = 0; i < tree.Children.Count; i++)
        {
            var child = tree.Children[i];

            if (child.IsNew)
            {
                tree.Children.RemoveAt(i);
                i--;
            }
            else
            {
                RemoveNewNodesRecursively(child);
            }
        }
    }

    private void SetNodesStatus(string[] nodeIds, ConnectionStatus status)
    {
        if (_tree is null)
            return;

        var allNodes = _tree.GetNodeAndDescendants().ToDictionary(n => n.Id, n => n);

        foreach (var nodeId in nodeIds)
        {
            if (allNodes.TryGetValue(nodeId, out var node))
            {
                node.Status = status;
            }
        }
    }

    private void SetTree(DeviceTreeRoot root, bool expandToOfflineNodes)
    {
        root.Name = CommonVocabulary.DevicePlural;

        DeviceTreeAdapter.SortNodeChildren(root.GetNodeAndDescendants());
        _tree = root;
        _adapter.SetDeviceTree(_tree, expandToOfflineNodes);
        _nodePaths = _tree.GetNodePaths();
    }

    private async Task SubscribeAllAsync(CancellationToken cancellationToken)
    {
        if (_gridNodes.Count == 0)
            return;

        var mapping = await DataCollectionWizardService.GetOutputConnectorMappingAsync();
        var mappingByNodeId = mapping.ToDictionary(m => m.ProcessDataId);

        var nodesToSubscribe = _service.FilteredGridItems
            .Select(lvrm => lvrm.DataNode)
            .OfType<IDeviceTreeLiveDataNode>()
            .ToArray();

        // Create all subscription tasks in parallel instead of awaiting each one
        var subscriptionTasks = nodesToSubscribe
            .Where(node => mappingByNodeId.TryGetValue(node.Id, out _))
            .Select(node => SubscribeToNodeAsync(node, mappingByNodeId, cancellationToken))
            .ToArray();

        await Task.WhenAll(subscriptionTasks);
    }

    private async Task SubscribeToNodeAsync(
        IDeviceTreeLiveDataNode node,
        Dictionary<string, ValueMappingEntry> mappingByNodeId,
        CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
            return;

        if (!mappingByNodeId.TryGetValue(node.Id, out var mapData))
            return;

        if (_handles.TryGetValue(node, out var valueTuple) && valueTuple.Item1 is not null)
            return;

        var item = _service.GridItems.FirstOrDefault(i => i.DataNode.Id == node.Id);
        if (item is null)
            return;

        IAsyncDisposable? processValueHandle = null;

        var culture = CultureInfo.CurrentCulture;

        try
        {
            Task ValueHandler(DateTime t, string? e)
            {
                item.Value = FormatValue(e, culture);
                item.LastUpdated = t > DateTime.MinValue
                    ? TimeZoneInfo.ConvertTime(t, TimeProvider.LocalTimeZone).ToString(culture)
                    : string.Empty;

                // Here rather than at render time: Refresh coalesces to twice a second, so counting there would
                // count pictures instead of values.
                _service.Session.Note();

                _service.Refresh();
                return Task.CompletedTask;
            }

            processValueHandle = await EventBroker.Subscribe(mapData.ValueOutputIdUI, ValueHandler, cancellationToken);
        }
        catch (Exception ex)
        {
            LogSubscribeTopicError(Logger, mapData.ValueOutputIdUI, node.Id, ex);
        }

        IAsyncDisposable? unitHandle = null;
        if (mapData.UnitOutputId is not null)
        {
            try
            {
                Task UnitHandler(DateTime _, string? e)
                {
                    if (e is not null)
                    {
                        var unit = JsonSerializer.Deserialize<string>(e);
                        if (unit is not null)
                        {
                            item.Unit = unit;
                            _service.Refresh();
                        }
                    }

                    return Task.CompletedTask;
                }

                unitHandle = await EventBroker.Subscribe(mapData.UnitOutputId.Value, UnitHandler, cancellationToken);
            }
            catch (Exception ex)
            {
                LogSubscribeTopicError(Logger, mapData.ValueOutputIdUI, $"{node.Id} (Unit)", ex);
            }
        }

        _handles[node] = (processValueHandle, unitHandle);
    }

    private async Task UpdateDeviceTreeAsync()
    {
        try
        {
            var masterDevices = _tree.Children.OfType<IDeviceTreeMasterNode>().ToArray();

            if (masterDevices.Length == 0 || masterDevices.All(v => v.Children.Count == 0))
            {
                _displayLoadingSpinner = false;
                await InvokeAsync(StateHasChanged);
                return;
            }

            await DataCollectionWizardService.RequestExistingDevicesAsync(masterDevices.Where(v => v.Children.Count > 0), false, async receivedDevices =>
            {
                // Only a scan that confirms a master online is handed to the builder. A master left out keeps its
                // last known structure, and the builder marks it and its children offline for lacking a counterpart.
                var onlineDevices = receivedDevices
                    .Select(r => r.device)
                    .Where(device => device?.Status == ConnectionStatus.Online)
                    .Cast<IDeviceTreeBase>();

                DeviceTreeBuilder.ExtendCurrentDeviceTree(_tree, [.. onlineDevices], []);

                RemoveNewNodesRecursively(_tree);

                SetTree(_tree, true);
                _adapter.Builder.Expansion.ChangeExpansionForLayers(true, 0, 0);

                _displayLoadingSpinner = false;
                await InvokeAsync(StateHasChanged);
            });
        }
        catch (Exception ex)
        {
            LogUpdateDeviceTreeError(Logger, ex);
        }
    }

    private async Task UnsubscribeAllAsync()
    {
        var subscriptionsToDelete = _handles.Values.ToList();
        _handles.Clear();

        var disposeTasks = new List<Task>();

        foreach (var (processValue, unit) in subscriptionsToDelete)
        {
            if (processValue is not null)
                disposeTasks.Add(processValue.DisposeAsync().AsTask());

            if (unit is not null)
                disposeTasks.Add(unit.DisposeAsync().AsTask());
        }

        await Task.WhenAll(disposeTasks);
    }
}
