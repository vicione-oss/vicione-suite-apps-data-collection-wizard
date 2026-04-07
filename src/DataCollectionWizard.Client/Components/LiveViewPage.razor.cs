using System.Collections.Concurrent;
using System.ComponentModel;
using System.Globalization;
using System.Text.Json;
using ClusterManagement.Public.DataflowEvents;
using DataCollectionWizard.Client.Components.LiveGrid.Models;
using DataCollectionWizard.Client.Components.LiveGrid.Services;
using DataCollectionWizard.Client.Extensions;
using DataCollectionWizard.Client.Services;
using DataCollectionWizard.Internal.Services;
using DataCollectionWizard.Public.Extensions;
using Microsoft.AspNetCore.Components;
using Sdk.Client.Modules;
using ViciOne.Driver.IoTCore.Contracts.DeviceTree;
using ViciOne.Driver.IoTCore.Contracts.DeviceTree.Extensions;
using ViciOne.Ui.Blazor.Components.Dialog.Components;
using ViciOne.Ui.Blazor.Components.LoadingSpinner.Factories;
using ViciOne.Ui.Blazor.Components.LoadingSpinner.Models;

namespace DataCollectionWizard.Client.Components;

public sealed partial class LiveViewPage : ModulePageBase<DataCollectionWizardClientModule>
{
    private readonly DeviceTreeAdapter _adapter = new(true);
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
            Message = Localization.DataCollectionWizardPage.SpinnerMessageTriggeringDeviceScan,
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
    private SemaphoreSlim? _semaphore = new(1, 1);
    private readonly LiveGridService _service = new();
    private DeviceTreeRoot _tree = new();

    public bool ShowMessageCommandWithEventCallback { get; set; }
    public bool ShowMessageSimpleCommand { get; set; }

    protected override string PageTitle => Localization.LiveViewPage.Title;

    [Inject] private IDataCollectionWizardService DataCollectionWizardService { get; set; } = null!;
    [Inject] private IEventBroker EventBroker { get; set; } = null!;

    [Inject(Key = Sdk.Constants.ClientTimeProviderServiceKey)]
    private TimeProvider TimeProvider { get; set; } = default!;

    private LiveGridRowModel[] CalculateGridItems(List<IDeviceTreeLiveDataNode> nodes)
    {
        return [.. _adapter.GetRelevantDataNodes()
            .Where(n => n.Visible && n.DataType.SupportedForLiveView())
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
        _service.PropertyChanged -= OnServicePropertyChangedAsync;
        _service.RebrowseRequested -= OnRebrowse;

        _cancelSubscribing.Dispose();

        DataCollectionWizardService.NodesOnline -= NodesOnline;
        DataCollectionWizardService.NodesOffline -= NodesOffline;

        await UnsubscribeAllAsync();

        _semaphore.Dispose();

        await base.DisposeInternal();
    }

    private async Task InitLiveView()
    {
        await DataCollectionWizardService.WaitForCurrentDeployment(new TimeSpan(0, 1, 0));
        await UpdateDeviceTreeAsync();

        if (!await DataCollectionWizardService.IsClusterRunningAsync())
        {
            await InvokeAsync(_refLatestClusterNotRunningDialog!.ShowAsync);
        }
    }

    private async Task NodesOffline(string[] arg)
    {
        if (_tree is null)
            return;

        SetNodesIsOffline(arg, true);
        SetTree(_tree, true);
        await InvokeAsync(StateHasChanged);
    }

    private async Task NodesOnline(string[] arg)
    {
        if (_tree is null)
            return;

        SetNodesIsOffline(arg, false);
        SetTree(_tree, false);
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
                LogAwaitingDeploymentWarning(Logger, ex.GetType().Name, ex.Message, ex.StackTrace ?? string.Empty);
            }
        });

    protected override async Task OnInitializedAsync()
    {
        _displayLoadingSpinner = true;

        _service.TreeBuilder.SetAdapter(_adapter);

        _service.PropertyChanged += OnServicePropertyChangedAsync;
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

    private void OnRebrowse()
    {
        _displayLoadingSpinner = true;
        InvokeAsync(StateHasChanged);
        _ = UpdateDeviceTreeAsync();
    }

    private async void OnServicePropertyChangedAsync(object? s, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(LiveGridService.FilteredGridItems))
        {
            await UnsubscribeAllAsync();
            await SubscribeAllAsync(_cancelSubscribing.Token);
        }
    }

    private async void OnTreeSelectionChangedAsync()
    {
        _cancelSubscribing.Cancel();
        var acquired = false;

        try
        {
            await _semaphore.WaitAsync();
            acquired = true;

            _cancelSubscribing.Dispose();
            _cancelSubscribing = new();
            await UnsubscribeAllAsync();
            _gridNodes = [.. _adapter.GetRelevantDataNodes().OfType<IDeviceTreeLiveDataNode>().Where(n => n.Visible)];
            _service.GridItems = CalculateGridItems(_gridNodes);
            _service.Refresh();
        }
        catch (Exception ex)
        {
            LogSubscribeAllError(Logger, ex.GetType().Name, ex.Message, ex.StackTrace ?? string.Empty);
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

    private void SetNodesIsOffline(string[] nodeIds, bool isOffline)
    {
        if (_tree is null)
            return;

        var allNodes = _tree.GetNodeAndDescendants().ToDictionary(n => n.Id, n => n);

        foreach (var nodeId in nodeIds)
        {
            if (allNodes.TryGetValue(nodeId, out var node))
            {
                node.IsOffline = isOffline;
            }
        }
    }

    private void SetTree(DeviceTreeRoot root, bool expandToOfflineNodes)
    {
        root.Name = Localization.DataCollectionWizardPage.Devices;

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
        var nodesToSubscribe = _service.FilteredGridItems
            .Select(lvrm => lvrm.DataNode)
            .OfType<IDeviceTreeLiveDataNode>()
            .ToArray();

        foreach (var node in nodesToSubscribe)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            var mapData = mapping.FirstOrDefault(e => e.ProcessDataId == node.Id);
            if (mapData is null)
                continue;

            if (_handles.TryGetValue(node, out var valueTuple) && valueTuple.Item1 is not null)
                return;

            IAsyncDisposable? processValueHandle = null;

            try
            {
                processValueHandle = await EventBroker.Subscribe(mapData.ValueOutputIdUI, (t, e) =>
                {
                    var item = _service.GridItems.First(i => i.DataNode.Id == node.Id);
                    if (e is not null)
                        item.Value = e;

                    item.LastUpdated = t > DateTime.MinValue
                        ? item.LastUpdated = TimeZoneInfo.ConvertTime(t, TimeProvider.LocalTimeZone).ToString(CultureInfo.CurrentCulture)
                        : item.LastUpdated = string.Empty;

                    _service.Refresh();
                    return Task.CompletedTask;
                });
            }
            catch (Exception ex)
            {
                LogSubscribeTopicError(Logger, mapData.ValueOutputIdUI, node.Id, ex.GetType().Name, ex.Message, ex.StackTrace ?? string.Empty);
            }

            IAsyncDisposable? unitHandle = null;
            if (mapData.UnitOutputId is not null)
            {
                try
                {
                    unitHandle = await EventBroker.Subscribe(mapData.UnitOutputId.Value, (_, e) =>
                    {
                        if (e is not null)
                        {
                            var unit = JsonSerializer.Deserialize<string>(e);
                            if (unit is not null)
                            {
                                _service.GridItems.First(i => i.DataNode.Id == node.Id).Unit = unit;
                                _service.Refresh();
                            }
                        }

                        return Task.CompletedTask;
                    });
                }
                catch (Exception ex)
                {
                    LogSubscribeTopicError(Logger, mapData.ValueOutputIdUI, $"{node.Id} (Unit)", ex.GetType().Name, ex.Message, ex.StackTrace ?? string.Empty);
                }
            }

            _handles[node] = (processValueHandle, unitHandle);
        }
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
                DeviceTreeBuilder.ExtendCurrentDeviceTree(_tree, [.. receivedDevices.Select(d => d.device).Where(d => d is not null && !d.IsOffline).Cast<IDeviceTreeBase>()], []);

                RemoveNewNodesRecursively(_tree);

                SetTree(_tree, true);
                _adapter.Builder.Expansion.ChangeExpansionForLayers(true, 0, 0);

                _displayLoadingSpinner = false;
                await InvokeAsync(StateHasChanged);
            });
        }
        catch (Exception ex)
        {
            LogUpdateDeviceTreeError(Logger, ex.GetType().Name, ex.Message, ex.StackTrace ?? string.Empty);
        }
    }

    private async Task UnsubscribeAllAsync()
    {
        var subscriptionsToDelete = _handles.Values.ToList();
        _handles.Clear();

        foreach (var (processValue, unit) in subscriptionsToDelete)
        {
            if (processValue is not null)
                await processValue.DisposeAsync();

            if (unit is not null)
                await unit.DisposeAsync();
        }
    }
}
