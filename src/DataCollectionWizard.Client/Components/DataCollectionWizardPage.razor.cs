using System.Globalization;
using System.Net;
using System.Net.Sockets;
using ClusterManagement.Public.Services;
using DataCollectionWizard.Client.Components.ManagementGrid.Models;
using DataCollectionWizard.Client.Components.ManagementGrid.Services;
using DataCollectionWizard.Client.Models.DeviceTree;
using DataCollectionWizard.Client.Services;
using DataCollectionWizard.Internal.Commands;
using DataCollectionWizard.Internal.Contracts;
using DataCollectionWizard.Internal.Events;
using DataCollectionWizard.Internal.Extensions;
using DataCollectionWizard.Internal.Services;
using DataCollectionWizard.Internal.Services.CloudDataflowGenerators;
using DataCollectionWizard.Public.Extensions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.JSInterop;
using Sdk.Client.Infrastructure;
using Sdk.Client.Modules;
using Sdk.Client.Services;
using Sdk.Connections.Contracts;
using Sdk.MessageBanner.Contracts;
using ViciOne.Driver.IoTCore.Contracts.Constants;
using ViciOne.Driver.IoTCore.Contracts.Dcp;
using ViciOne.Driver.IoTCore.Contracts.DeviceTree;
using ViciOne.Driver.IoTCore.Contracts.DeviceTree.Extensions;
using ViciOne.Ui.Blazor.Components.Dialog.Components;
using ViciOne.Ui.Blazor.Components.LoadingSpinner.Factories;
using ViciOne.Ui.Blazor.Components.LoadingSpinner.Models;
using ViciOne.Ui.Blazor.Components.TextBox;

namespace DataCollectionWizard.Client.Components;

public sealed partial class DataCollectionWizardPage : ModulePageBase<DataCollectionWizardClientModule>, IEventConsumer<DeviceTreeApplicationEvent>
{
    private const string IoLinkMasterDialogHeightNormal = "350px";
    private const string IoLinkMasterDialogHeightList = "600px";
    private const int MaxRecommendedDataPoints = 100;
    private const double MaxRecommendedMessageDisplayBoundary = 0.6;

    private readonly DeviceTreeAdapter _adapter = new(false);
    private Dictionary<string, IDeviceTreeBase> _allNodes = [];
    private readonly List<IDeviceTreeMasterNode> _changedMasterDevices = [];
    private int _currentlyEnabledDataPoints;
    private readonly List<IDeviceTreeBase> _deletedNodes = [];
    private NodeBase? _deletingNode;
    private NodeBase? _deletingNodeParent;
    private string _deviceAlias = string.Empty;
    private bool _displayLoadingSpinner;
    private NodeBase? _editingNode;
    private string _newIoLinkMasterAddress = string.Empty;
    private string _newVSEAddress = string.Empty;
    private Dictionary<IDeviceTreeBase, List<IDeviceTreeBase>>? _nodePaths;
    private readonly TimedMessage[] _loadingSpinnerMessagesAwaitingDeployment = [
        new()
        {
            Message = Localization.DataCollectionWizardPage.SpinnerMessageAwaitingCurrentDeployments,
        },
    ];
    private readonly TimedMessage[] _loadingSpinnerMessagesInitializing = [
        new()
        {
            DisplayDuration = 3,
            Message = Localization.DataCollectionWizardPage.SpinnerMessageAwaitingCurrentDeployments,
        },
        TimedMessageFactory.CreateGap(1),
        new()
        {
            DisplayDuration = 3,
            Message = Localization.DataCollectionWizardPage.SpinnerMessageCreatingScanEngine,
        },
        TimedMessageFactory.CreateGap(1),
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
    private readonly TimedMessage[] _loadingSpinnerMessagesScanDevice = [
        new()
        {
            DisplayDuration = 3,
            Message = Localization.DataCollectionWizardPage.SpinnerMessageGeneratingDataflow,
        },
        TimedMessageFactory.CreateGap(1),
        new()
        {
            DisplayDuration = 3,
            Message = Localization.DataCollectionWizardPage.SpinnerMessageDeployingDataflow,
        },
        TimedMessageFactory.CreateGap(1),
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
    private readonly TimedMessage[] _loadingSpinnerMessagesScanIoLink =
    [
        new()
        {
            DisplayDuration = 3,
            Message = Localization.DataCollectionWizardPage.SpinnerMessageTriggeringIoLinkScan,
        },
        TimedMessageFactory.CreateGap(1),
        new()
        {
            Message = Localization.DataCollectionWizardPage.SpinnerMessageWaitingForScanResult,
        },
    ];
    private readonly TimedMessage[] _loadingSpinnerMessagesUpdateDeviceTree = [
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
    private TimedMessage[] _loadingSpinnerMessages = [];
    private readonly List<Connection> _publishTargets = [];
    private List<PublishTargetInfo> _publishTargetInfos = [];
    private Dialog? _refAddIoLinkMasterDialog;
    private TextBox? _refAddIoLinkMasterTextBox;
    private Dialog? _refAddVSEDialog;
    private TextBox? _refAddVSETextBox;
    private Dialog? _refAliasDialog;
    private TextBox? _refAliasTextBox;
    private Dialog? _refDataInvalidDialog;
    private Dialog? _refDeleteDialog;
    private Dialog? _refDeleteAllOfflineDialog;

    private List<DcpDevice>? _scannedIoLinkDevices;
    private readonly List<string> _selectedIoLinkDevices = [];
    private DeviceTreeRoot? _tree;
    private readonly Lock _treeLock = new();
    private string _ioLinkMasterDialogHeight = IoLinkMasterDialogHeightNormal;
    private int _ioLinkMasterTabIndex;
    private CancellationTokenSource _dcwScanTokenSource = new();
    private bool _isIoLinkMasterUriValid = true;
    private bool _isVSEUriValid = true;
    private bool _isIoLinkMasterUriUnique = true;
    private bool _isVSEUriUnique = true;
    private int _rawDataPullingMaxTimesADay = 12;
    private string _ioLinkMasterFilter = string.Empty;
    private List<string> _scanIoLinkErrors = [];
    private string _saveReasons = string.Empty;
    private bool _gridNeedsRebuild;
    private readonly ManagementGridService _service = new();
    private IDisposable? _subscriptionHandleDeviceTreeApplication;

    protected override string PageTitle => Localization.DataCollectionWizardPage.Title;

    [Inject] private IConnectionService ConnectionService { get; set; } = null!;
    [Inject] private IEnumerable<ICloudFilter> CloudFilters { get; set; } = null!;
    [Inject] private IDataCollectionWizardService DataCollectionWizardService { get; set; } = null!;
    [Inject] private IResourceDownloadStateService ResourceDownloadState { get; set; } = null!;
    [Inject] private IJSRuntime Js { get; set; } = default!;
    [Inject] private IMessageBannerService MessageBannerService { get; set; } = default!;
    [Inject] private IUiMediator Mediator { get; set; } = default!;

    public Task Consume(ClientContext<DeviceTreeApplicationEvent> context, CancellationToken cancellationToken)
    {
        _service.DisableClusterActions = false;
        return Task.CompletedTask;
    }

    private Task AddDeviceToDeviceTree(IDeviceTreeMasterNode device)
    {
        if (_tree is null)
            throw new InvalidOperationException("Tree is null");

        if (TryGetExistingDeviceTreeMaster(device, out var existingDevice))
        {
            // sollte nicht auftreten da beim Hinzufügen Dialog auf bestehende VSEs geprüft wird
            LogUnexpectedUpdateWarning(Logger, existingDevice!.Id);

            lock (_treeLock)
            {
                _tree.Children.Remove(existingDevice);
            }
        }
        else
        {
            foreach (var node in device.GetNodeAndDescendants())
            {
                node.IsNew = true;
            }
        }

        var allNodes = device.GetNodeAndDescendants().ToArray();

        foreach (var dataNode in allNodes.OfType<IDeviceTreeDataNode>().ToArray())
        {
            dataNode.AddConfigurations(_publishTargets);
        }

        lock (_treeLock)
        {
            _tree.Children.Add(device);
        }

        SetTree(_tree, false);

        // select newly added node
        _service.TreeBuilder.Selection.ChangeSelection(
            _service.TreeBuilder.RootNodes.First().Children.Single(c => ((NodeBase)c.TreeNode).Device == device).TreeNode,
            select: true,
            deselectOthers: true
        );

        _service.DeviceTreeChanged = true;
        _changedMasterDevices.Add(device);

        _saveReasons += " new devices have been added;";

        return Task.CompletedTask;
    }

    private void CancelDcwScan()
    {
        _dcwScanTokenSource.Cancel();
        _dcwScanTokenSource.Dispose();
        _dcwScanTokenSource = new();
    }

    private void CheckDataPointRecommendedLimit(bool recalculate = false)
    {
        if (recalculate && _tree is not null)
        {
            lock (_treeLock)
            {
                var treeNodes = _tree.GetNodeAndDescendants().ToArray();
                _currentlyEnabledDataPoints = DeviceTreeAdapter.CountEnabledDataPoints(treeNodes);
            }
        }

        var dataPointPrecentageToRecommended = 1.0 * _currentlyEnabledDataPoints / MaxRecommendedDataPoints;
        if (dataPointPrecentageToRecommended is >= MaxRecommendedMessageDisplayBoundary and < 1.0)
        {
            MessageBannerService.ShowMessageBanner(MessageType.Information, string.Format(
                CultureInfo.InvariantCulture,
                Localization.DataCollectionWizardPage.DataPointLimitApproaching,
                _currentlyEnabledDataPoints,
                MaxRecommendedDataPoints
            ));
        }
        else if (dataPointPrecentageToRecommended >= 1.0)
        {
            MessageBannerService.ShowMessageBanner(MessageType.Warning, string.Format(
                CultureInfo.InvariantCulture,
                Localization.DataCollectionWizardPage.DataPointLimitReached,
                _currentlyEnabledDataPoints,
                MaxRecommendedDataPoints
            ));
        }
        else
        {
            MessageBannerService.CloseMessageBanner();
        }
    }

    private async Task ConfirmLeave(LocationChangingContext context)
    {
        if (!_service.DeviceTreeChanged)
            return;

        var confirmed = await Js.InvokeAsync<bool>("window.confirm", Localization.DataCollectionWizardPage.UnsavedChanges);
        if (!confirmed)
        {
            context.PreventNavigation();
        }
    }

    private void FillPublishTargets()
    {
        _publishTargets.Clear();
        _publishTargets.AddRange(PublishTargetsFilter.GetPublishTargets(ConnectionService.Connections, CloudFilters));

        var moneoFilter = new MoneoCloudFilter();
        _publishTargetInfos = [.. _publishTargets.Select(c =>
        {
            var kind = AnnaCloudFilter.IsAnnaConnection(c) ? ConnectionKind.Anna
                     : moneoFilter.GetCloudConnections([c]).Any() ? ConnectionKind.Moneo
                     : ConnectionKind.Unsupported;
            return new PublishTargetInfo(c, kind);
        })];
    }

    private IEnumerable<DcpDevice> FilterScannedDevices(IEnumerable<DcpDevice> devices)
        => devices.Where(d =>
        {
            if (string.IsNullOrWhiteSpace(_ioLinkMasterFilter))
                return true;

            return (d.Address?.ToString()?.Contains(_ioLinkMasterFilter, StringComparison.OrdinalIgnoreCase) ?? false)
                || d.DeviceName.Contains(_ioLinkMasterFilter, StringComparison.OrdinalIgnoreCase)
                || d.MacAddress.Contains(_ioLinkMasterFilter, StringComparison.OrdinalIgnoreCase)
                || d.VendorId.ToString(CultureInfo.InvariantCulture).Contains(_ioLinkMasterFilter, StringComparison.OrdinalIgnoreCase)
                || d.DeviceId.ToString(CultureInfo.InvariantCulture).Contains(_ioLinkMasterFilter, StringComparison.OrdinalIgnoreCase);
        });

    protected override ValueTask DisposeInternal()
    {
        MessageBannerService.CloseMessageBanner();

        _service.AddNewIoLinkMasterRequested -= OnAddIoLinkMasterRequestedAsync;
        _service.AddNewVseRequested -= OnAddVSERequestedAsync;
        _service.DataPointEnabledChanged -= OnDataPointEnabledChanged;
        _service.DeleteOfflineNodesRequested -= OpenDeleteAllOfflineDialogAsync;
        _service.RebrowseRequested -= RebrowseButton;
        _service.SaveRequested -= SaveButtonAsync;
        _service.SetAllDatapointsEnabledRequested -= SetAllDatapointsEnabled;
        _service.SetCompressionForAllRequested -= SetAllDatapointsCompression;
        _service.SetDebugRawDataRequested -= SetDebugRawDataGrid;

        _adapter.NodeDeleted -= OnAdapterNodeDeleted;
        _adapter.NodeEdited -= OnAdapterNodeEdited;
        _adapter.SelectionChanged -= OnTreeSelectionChangedAsync;

        _dcwScanTokenSource.Dispose();

        DataCollectionWizardService.NodesOnline -= NodesOnline;
        DataCollectionWizardService.NodesOffline -= NodesOffline;
        DataCollectionWizardService.DataPossiblyInvalid -= OnDataPossiblyInvalid;

        _subscriptionHandleDeviceTreeApplication?.Dispose();
        return base.DisposeInternal();
    }

    private static bool HasChangedUnits(IDeviceTreeMasterNode masterNode, Dictionary<string, string?> oldStructureUnits)
    {
        foreach (var processData in masterNode.GetNodeAndDescendants().OfType<DeviceTreeProcessData>())
        {
            if (oldStructureUnits.TryGetValue(processData.Id, out var oldUnit) && oldUnit != processData.StructureUnit)
            {
                return true;
            }
        }

        return false;
    }

    private static Dictionary<IDeviceTreeBase, List<IDeviceTreeBase>> GetNodePaths(IEnumerable<DeviceTreeRoot> deviceTreeList)
    {
        var result = new List<KeyValuePair<IDeviceTreeBase, List<IDeviceTreeBase>>>();
        foreach (var deviceTree in deviceTreeList)
        {
            var deviceTreePaths = new Dictionary<IDeviceTreeBase, List<IDeviceTreeBase>>();
            GetNodePathsRecursively(deviceTree, deviceTreePaths, []);
            result.AddRange([.. deviceTreePaths]);
        }

        return result.ToDictionary(r => r.Key, r => r.Value);

        static void GetNodePathsRecursively(IDeviceTreeBase node, Dictionary<IDeviceTreeBase, List<IDeviceTreeBase>> deviceTreePaths, List<IDeviceTreeBase> path)
        {
            var newPath = new List<IDeviceTreeBase>(path) { node };
            deviceTreePaths[node] = path;

            foreach (var child in node.Children)
                GetNodePathsRecursively(child, deviceTreePaths, newPath);
        }
    }

    private async Task InitDcw()
    {
        try
        {
            await ResourceDownloadState.WaitForCompletion();
            await DataCollectionWizardService.WaitForCurrentDeployment(new TimeSpan(0, 1, 0));
            await DataCollectionWizardService.AddIoLinkScannerDataflow(_service.LogLevel);
            await UpdateDeviceTreeAsync(false, false);
        }
        catch (Exception ex)
        {
            LogInitDcwError(Logger, ex.GetType().Name, ex.Message, ex.StackTrace);
            await InvokeAsync(() => MessageBannerService.ShowMessageBanner(MessageType.Warning, Localization.DataCollectionWizardPage.Error));
        }
    }

    private bool IsIoLinkMasterDialogOkEnabled()
        => _ioLinkMasterTabIndex == 0
            ? !string.IsNullOrWhiteSpace(_newIoLinkMasterAddress) && _isIoLinkMasterUriValid && _isIoLinkMasterUriUnique
            : _selectedIoLinkDevices.Count > 0;

    private void SetIoLinkMasterDialogScanHeight(int tabIndex)
    {
        _ioLinkMasterDialogHeight = tabIndex == 1 ? IoLinkMasterDialogHeightList : IoLinkMasterDialogHeightNormal;
        InvokeAsync(StateHasChanged);
    }

    private void SetTree(DeviceTreeRoot root, bool expandToOfflineNodes)
    {
        root.Name = Localization.DataCollectionWizardPage.Devices;

        lock (_treeLock)
        {
            SetTreeCore(root, expandToOfflineNodes);
        }

        CheckDataPointRecommendedLimit(true);
    }

    /// <summary>
    /// Must be called while holding <see cref="_treeLock"/>.
    /// </summary>
    private void SetTreeCore(DeviceTreeRoot root, bool expandToOfflineNodes)
    {
        var treeNodes = root.GetNodeAndDescendants().ToArray();
        DeviceTreeAdapter.SortNodeChildren(treeNodes);
        DeviceTreeAdapter.SortEventTriggers(treeNodes);

        _tree = root;
        _allNodes = _tree.GetNodeAndDescendants().ToDictionary(n => n.Id, n => n);

        _nodePaths = GetNodePaths([_tree]);
        _adapter.SetDeviceTree(_tree, expandToOfflineNodes);
        _service.HasOfflineNodes = _tree.GetNodeAndDescendants().Any(n => n.IsOffline && n is not IDeviceTreeMasterNode);

        foreach (var dataNode in treeNodes.OfType<IDeviceTreeDataNode>())
            dataNode.AddConfigurations(_publishTargets);
    }

    private bool TryGetExistingDeviceTreeMaster(IDeviceTreeMasterNode device, out IDeviceTreeMasterNode? existingDevice)
        => TryGetExistingDeviceTreeMaster(device, _tree!.Children.OfType<IDeviceTreeMasterNode>(), out existingDevice);

    private static bool TryGetExistingDeviceTreeMaster(IDeviceTreeMasterNode device, IEnumerable<IDeviceTreeMasterNode> existingMasterNodes, out IDeviceTreeMasterNode? existingDevice)
    {
        existingDevice = existingMasterNodes.FirstOrDefault(v => device.Url == v.Url);
        return existingDevice is not null;
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

    private async Task OnAddVSEDialogCloseAsync()
        => await _refAddVSEDialog!.CloseAsync();

    private void OnAdapterNodeDeleted(NodeBase node, NodeBase? parent)
    {
        //root deleted?
        if (parent is null)
            return;

        _deletingNode = node;
        _deletingNodeParent = parent;
        _refDeleteDialog!.ShowAsync();
    }

    private void OnAdapterNodeEdited(NodeBase node)
    {
        if (node.Device is not IDeviceTreeAliasNode aliasNode)
            return;

        _deviceAlias = aliasNode.NameAlias ?? string.Empty;
        _editingNode = node;
        _refAliasDialog!.ShowAsync();
    }

    private async Task OnAliasDialogVisibleChanged(bool visible)
    {
        if (visible)
            await OnAliasDialogShownAsync();
    }

    private async Task OnAliasDialogShownAsync()
        => await _refAliasTextBox!.FocusAsync();

    private async Task OnAliasDialogTextBoxEnterPressedAsync(string? _)
        => await OnAliasDialogOkAsync();

    private async Task OnAddIoLinkMasterDialogVisibleChangedAsync(bool visible)
    {
        if (visible)
            await OnAddIoLinkMasterDialogShownAsync();
    }

    private async Task OnAddIoLinkMasterDialogShownAsync()
        => await _refAddIoLinkMasterTextBox!.FocusAsync();

    private async Task OnAddIoLinkMasterTextBoxEnterPressedAsync(string? _)
    {
        _isIoLinkMasterUriValid = true;
        _isIoLinkMasterUriUnique = true;

        await OnAddIoLinkMasterDialogOkAsync();
    }

    private async void OnAddIoLinkMasterRequestedAsync()
    {
        _ioLinkMasterTabIndex = 0;
        _dcwScanTokenSource = new CancellationTokenSource();
        ScanIoLinkDevices(_dcwScanTokenSource.Token);

        await _refAddIoLinkMasterDialog!.ShowAsync();
    }

    private async void OnAddVSEDialogVisibleChangedAsync(bool visible)
    {
        if (visible)
            await OnAddVSEDialogShownAsync();
    }

    private async Task OnAddVSEDialogShownAsync()
        => await _refAddVSETextBox!.FocusAsync();

    private async Task OnAddVSETextBoxEnterPressedAsync(string? _)
    {
        _isVSEUriValid = true;
        _isVSEUriUnique = true;

        await OnAddVSEDialogOkAsync();
    }

    private async void OnAddVSERequestedAsync()
        => await _refAddVSEDialog!.ShowAsync();

    private async Task OnAddIoLinkMasterDialogCloseAsync()
    {
        CancelDcwScan();
        await _refAddIoLinkMasterDialog!.CloseAsync();
    }

    private async Task OnAddIoLinkMasterDialogOkAsync()
    {
        CancelDcwScan();
        _newIoLinkMasterAddress = _newIoLinkMasterAddress.Trim();

        if (_ioLinkMasterTabIndex == 0)
        {
            Uri? newIoLinkUri = null;

            try
            {
                _isIoLinkMasterUriValid = IsValidHost(ExtractHost(_newIoLinkMasterAddress));
                if (_isIoLinkMasterUriValid)
                {
                    var uriBuilder = new UriBuilder(_newIoLinkMasterAddress);
                    _isIoLinkMasterUriValid = uriBuilder.Uri.Port > 0;
                    newIoLinkUri = uriBuilder.Uri;
                }
            }
            catch
            {
                _isIoLinkMasterUriValid = false;
            }

            if (_isIoLinkMasterUriValid)
            {
                lock (_treeLock)
                {
                    _isIoLinkMasterUriUnique = !_tree!.Children.OfType<DeviceTreeIoLinkMaster>().Any(v => v.Url.Equals(newIoLinkUri));
                }
            }

            if (!_isIoLinkMasterUriValid || !_isIoLinkMasterUriUnique)
            {
                await InvokeAsync(StateHasChanged);
                return;
            }

            _selectedIoLinkDevices.Clear();
            _selectedIoLinkDevices.Add(_newIoLinkMasterAddress);
        }

        _loadingSpinnerMessages = _loadingSpinnerMessagesScanDevice;
        _displayLoadingSpinner = true;
        await _refAddIoLinkMasterDialog!.CloseAsync();
        var numberOfReceivedDevices = 0;

        var deviceEngineInfos = _selectedIoLinkDevices.Select(d => new DeviceEngineInfo(new UriBuilder(d).Uri, typeof(DeviceTreeIoLinkMaster).AssemblyQualifiedName!));

        await DataCollectionWizardService.RequestNewDevicesDeviceTreeAsync(
            deviceEngineInfos,
            async (d, _, a) =>
            {
                var uri = new UriBuilder(a).Uri;

                if (d is not DeviceTreeIoLinkMaster)
                {
                    LogUnexpectedNullDeviceError(Logger, uri.AbsoluteUri);
                    d = new DeviceTreeIoLinkMaster
                    {
                        Description = new DeviceTreeNodeDescription
                        {
                            Text = "an ifm IO-Link device",
                        },
                        Id = $"IoLink@{uri.DnsSafeHost}:{uri.Port}",
                        IsOffline = true,
                        MacAddress = "ff:ff:ff:ff:ff",
                        Name = "IO-Link Master",
                        Url = uri,
                    };
                }

                await AddDeviceToDeviceTree(d);
                numberOfReceivedDevices++;

                if (numberOfReceivedDevices >= _selectedIoLinkDevices.Count)
                {
                    _displayLoadingSpinner = false;
                    await InvokeAsync(StateHasChanged);
                }
            },
            true,
            _service.LogLevel);
    }

    private async Task OnAddVSEDialogOkAsync()
    {
        _newVSEAddress = _newVSEAddress.Trim();
        Uri? vseAddress = null;

        try
        {
            _isVSEUriValid = IsValidHost(ExtractHost(_newVSEAddress));
            if (_isVSEUriValid)
            {
                var uriBuilder = new UriBuilder(_newVSEAddress);
                _isVSEUriValid = uriBuilder.Uri.Port > 0;
                vseAddress = uriBuilder.Uri.SetVsePort();
            }
        }
        catch
        {
            _isVSEUriValid = false;
        }

        if (_isVSEUriValid)
        {
            lock (_treeLock)
            {
                _isVSEUriUnique = !_tree!.Children.OfType<DeviceTreeVseDevice>().Any(v => v.Url == vseAddress);
            }
        }

        if (!_isVSEUriValid || !_isVSEUriUnique)
        {
            await InvokeAsync(StateHasChanged);
            return;
        }

        _loadingSpinnerMessages = _loadingSpinnerMessagesScanDevice;
        _displayLoadingSpinner = true;
        await _refAddVSEDialog!.CloseAsync();

        await DataCollectionWizardService.RequestNewDeviceDeviceTreeAsync(typeof(DeviceTreeVseDevice), vseAddress!, async (d, _, _) =>
        {
            if (d is not DeviceTreeVseDevice)
            {
                LogUnexpectedNullDeviceError(Logger, vseAddress!.DnsSafeHost);
                d = new DeviceTreeVseDevice
                {
                    Description = new DeviceTreeNodeDescription
                    {
                        Text = "an ifm VSE device",
                    },
                    Id = $"vse@{vseAddress}",
                    IsOffline = true,
                    MacAddress = "ff:ff:ff:ff:ff",
                    Name = "VSE Device",
                    Url = VseAddresses.GetVseAddressWithPort(vseAddress.DnsSafeHost),
                };
            }

            await AddDeviceToDeviceTree(d);
            _displayLoadingSpinner = false;
            await InvokeAsync(StateHasChanged);
        }, true, _service.LogLevel);
    }

    private async Task OnAliasDialogCloseAsync()
        => await _refAliasDialog!.CloseAsync();

    private async Task OnAliasDialogOkAsync()
    {
        await _refAliasDialog!.CloseAsync();

        if (_editingNode!.Device is not IDeviceTreeAliasNode aliasNode)
            return;

        if (_deviceAlias == aliasNode.NameAlias)
            return;

        aliasNode.NameAlias = string.IsNullOrWhiteSpace(_deviceAlias) ? null : _deviceAlias.Trim();
        _service.DeviceTreeChanged = true;

        _editingNode.DisplayText = DeviceTreeNodeNameProvider.GetTreeDisplayText(_editingNode.Device);
        _adapter.Builder.Notifications.NotifyNodeChanged(_editingNode);

        _editingNode = null;
    }

    private async Task OnDeleteAllOfflineDialogCloseAsync()
        => await _refDeleteAllOfflineDialog!.CloseAsync();

    private async Task OnDataInvalidDialogOkAsync()
        => await _refDataInvalidDialog!.CloseAsync();

    private void OnDataPointEnabledChanged(bool enabled)
    {
        _currentlyEnabledDataPoints += enabled ? 1 : -1;
        CheckDataPointRecommendedLimit();
    }

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

                _loadingSpinnerMessages = _loadingSpinnerMessagesAwaitingDeployment;
                _displayLoadingSpinner = true;
                await InvokeAsync(StateHasChanged);
                await DataCollectionWizardService.WaitForCurrentDeployment(new TimeSpan(0, 1, 0));
                _displayLoadingSpinner = false;

                await InvokeAsync(StateHasChanged);
            }
            catch (Exception ex)
            {
                LogAwaitingDeploymentWarning(Logger, ex.GetType().Name, ex.Message, ex.StackTrace ?? string.Empty);
            }
        });

    private async Task OnDeleteAllOfflineDialogOkAsync()
    {
        lock (_treeLock)
        {
            foreach (var device in _tree!.Children.OfType<IDeviceTreeMasterNode>())
            {
                var deletedNodes = RemoveOfflineNodesRecursively(device);

                if (deletedNodes.Length > 0)
                {
                    _changedMasterDevices.Add(device);

                    _saveReasons += " nodes have been deleted;";
                }

                _deletedNodes.AddRange(deletedNodes);
            }
        }

        SetTree(_tree, false);
        _service.DeviceTreeChanged = true;

        await _refDeleteAllOfflineDialog!.CloseAsync();
    }

    private async Task OnDeleteDialogCloseAsync()
        => await _refDeleteDialog!.CloseAsync();

    private async Task OnDeleteDialogOkAsync()
    {
        lock (_treeLock)
        {
            var masterDevices = _tree!.GetNodeAndDescendants()
                         .OfType<IDeviceTreeMasterNode>()
                         .ToDictionary(m => m, m => m.GetNodeAndDescendants().ToList());

            var parentMaster = masterDevices.FirstOrDefault(m => m.Value.Any(n => n.Id == _deletingNode!.Device.Id)).Key;

            _service.DeviceTreeChanged = true;
            _adapter.RemoveNodeFromParent(_deletingNode!, _deletingNodeParent);
            DeviceTreeBuilder.RemoveEventTriggers(_tree!, _deletingNode!.Device);

            _deletedNodes.Add(_deletingNode!.Device);
            _changedMasterDevices.Add(parentMaster!);
            _saveReasons += " nodes have been deleted;";
        }

        await _refDeleteDialog!.CloseAsync();

        SetGridItems();
        CheckDataPointRecommendedLimit(true);

        _deletingNode = null;
        _deletingNodeParent = null;
    }

    private void OnDeviceTreeChanged(IDeviceTreeMasterNode masterNode)
    {
        if (!_changedMasterDevices.Contains(masterNode))
            _changedMasterDevices.Add(masterNode);

        _saveReasons += " devicetree has changed;";

        _service.DeviceTreeChanged = true;
    }

    protected override async Task OnInitializedAsync()
    {
        _loadingSpinnerMessages = _loadingSpinnerMessagesInitializing;
        _displayLoadingSpinner = true;

        _service.TreeBuilder.SetAdapter(_adapter);

        _service.AddNewIoLinkMasterRequested += OnAddIoLinkMasterRequestedAsync;
        _service.AddNewVseRequested += OnAddVSERequestedAsync;
        _service.DeleteOfflineNodesRequested += OpenDeleteAllOfflineDialogAsync;
        _service.DataPointEnabledChanged += OnDataPointEnabledChanged;
        _service.RebrowseRequested += RebrowseButton;
        _service.SaveRequested += SaveButtonAsync;
        _service.SetAllDatapointsEnabledRequested += SetAllDatapointsEnabled;
        _service.SetCompressionForAllRequested += SetAllDatapointsCompression;
        _service.SetDebugRawDataRequested += SetDebugRawDataGrid;
        FillPublishTargets();

        SetTree(await DataCollectionWizardService.RequestDeviceTreeAsync(), false);
        _adapter.NodeDeleted += OnAdapterNodeDeleted;
        _adapter.NodeEdited += OnAdapterNodeEdited;

        _adapter.SelectionChanged += OnTreeSelectionChangedAsync;

        DataCollectionWizardService.DataPossiblyInvalid += OnDataPossiblyInvalid;
        DataCollectionWizardService.NodesOnline += NodesOnline;
        DataCollectionWizardService.NodesOffline += NodesOffline;

        _subscriptionHandleDeviceTreeApplication = Mediator.Register(this);

#if DEBUG
        _newVSEAddress = "10.45.24.101";
#endif
        _ = Task.Run(InitDcw);
    }

    protected override void OnParametersSet()
        => FillPublishTargets();

    private void OnRescanIoLinkDevicesClicked()
        => ScanIoLinkDevices(_dcwScanTokenSource.Token);

    private void OnScannedDeviceSelectionChanged(bool selected, DcpDevice device)
    {
        var deviceAddress = device.Address?.ToString();

        if (deviceAddress is null)
        {
            LogMissingAddressSelectedWarning(Logger);
            return;
        }

        if (selected)
            _selectedIoLinkDevices.Add(deviceAddress);
        else
            _selectedIoLinkDevices.Remove(deviceAddress);
    }

    private void OnTreeSelectionChangedAsync()
    {
        // SelectionChanged can fire from a background thread (e.g. DataflowEventBroker
        // calling SetDeviceTree inside UpdateDeviceTreeAsync), so InvokeAsync is required
        // to marshal back to the Blazor circuit dispatcher before touching component state.
        //
        // Two-phase render to show the tree selection highlight before the grid is rebuilt:
        //   Phase 1 — clear the grid and queue a render via StateHasChanged. Blazor will
        //             include both the empty grid and the sidebar's own selection-highlight
        //             render in the same batch and send it to the browser.
        //   Phase 2 — OnAfterRenderedAsync is invoked only after that batch has been sent,
        //             so SetGridItems() always runs in a subsequent render cycle.
        //
        // Note: if Blazor coalesces this StateHasChanged with another pending render
        // (e.g. a simultaneous node-online event), Phase 1 and Phase 2 may still appear
        // together. This is expected Blazor Server batching behaviour.
        _ = InvokeAsync(() =>
        {
            _service.GridItems = [];
            _gridNeedsRebuild = true;
            StateHasChanged();
        });
    }

    protected override Task OnAfterRenderedAsync(bool firstRender)
    {
        if (_gridNeedsRebuild)
        {
            // Clear the flag before SetGridItems so a re-entrant render does not loop.
            _gridNeedsRebuild = false;

            // Phase 2: rebuild grid items. SetGridItems sets _service.GridItems which
            // triggers the grid component to re-render itself via its own PropertyChanged
            // subscription — no page-level StateHasChanged needed here.
            SetGridItems();
        }

        return Task.CompletedTask;
    }

    private async void OpenDeleteAllOfflineDialogAsync()
        => await _refDeleteAllOfflineDialog!.ShowAsync();

    private async void RebrowseButton()
    {
        try
        {
            _loadingSpinnerMessages = _loadingSpinnerMessagesUpdateDeviceTree;
            _displayLoadingSpinner = true;

            await InvokeAsync(StateHasChanged);
            await UpdateDeviceTreeAsync(true, true);
        }
        catch (Exception ex)
        {
            LogRebrowseButtonError(Logger, ex.GetType().Name, ex.Message, ex.StackTrace ?? string.Empty);
        }
    }

    private static IDeviceTreeBase[] RemoveOfflineNodesRecursively(IDeviceTreeBase tree)
    {
        var result = new List<IDeviceTreeBase>();

        for (var i = 0; i < tree.Children.Count; i++)
        {
            var child = tree.Children[i];

            if (child.IsOffline && child is not IDeviceTreeMasterNode)
            {
                result.Add(child);
                tree.Children.RemoveAt(i);
                i--;
            }
            else
            {
                result.AddRange(RemoveOfflineNodesRecursively(child));
            }
        }

        return [.. result];
    }

    private void ResetAddIoLinkMasterDialog()
    {
        _ioLinkMasterFilter = string.Empty;
        _newIoLinkMasterAddress = string.Empty;
        _isIoLinkMasterUriUnique = true;
        _isIoLinkMasterUriValid = true;
        _ioLinkMasterDialogHeight = IoLinkMasterDialogHeightNormal;
    }

    private void ResetAddVSEDialog()
    {
        _newVSEAddress = string.Empty;
        _isVSEUriUnique = true;
        _isVSEUriValid = true;
    }

    private async void SaveButtonAsync()
    {
        try
        {
            if (!_service.DeviceTreeChanged)
                return;

            _service.DisableClusterActions = true;
            _service.DeviceTreeChanged = false;
            _displayLoadingSpinner = true;

            await InvokeAsync(StateHasChanged);
            _ = Task.Run(async () =>
            {
                if (_tree is null)
                {
                    return;
                }

                try
                {
                    var changedMasters = _changedMasterDevices.Select(m => m.Id).ToArray();
                    var deletedNodes = _deletedNodes.ToArray();

                    _changedMasterDevices.Clear();
                    _deletedNodes.Clear();
                    _saveReasons = string.Empty;

                    lock (_treeLock)
                    {
                        var isNewUpdated = false;

                        foreach (var node in _tree.GetNodeAndDescendants())
                        {
                            isNewUpdated |= node.IsNew;
                            node.IsNew = false;
                        }

                        if (isNewUpdated)
                        {
                            _tree.Name = Localization.DataCollectionWizardPage.Devices;
                            SetTreeCore(_tree, false);
                        }
                    }

                    CheckDataPointRecommendedLimit(true);

                    await DataCollectionWizardService.SaveDeviceTreeAsync(changedMasters, deletedNodes, _tree!, _service.LogLevel);
                }
                catch (Exception ex)
                {
                    LogWhileSaveDeviceTreeError(Logger, ex.GetType().Name, ex.Message, ex.StackTrace ?? string.Empty);
                }
            });
        }
        catch (Exception ex)
        {
            LogWhileSaveDeviceTreeError(Logger, ex.GetType().Name, ex.Message, ex.StackTrace ?? string.Empty);
        }
    }

    private void ScanIoLinkDevices(CancellationToken cancellationToken)
    {
        _scannedIoLinkDevices = null;
        _scanIoLinkErrors = [];
        _selectedIoLinkDevices.Clear();

        DeviceTreeIoLinkMaster[] currentIoLinkMasters;
        lock (_treeLock)
        {
            currentIoLinkMasters = [.. _tree!.GetNodeAndDescendants().OfType<DeviceTreeIoLinkMaster>()];
        }

        _ = Task.Run(async () =>
        {
            try
            {
                var scanResult = await DataCollectionWizardService.ScanIoLinkDevicesAsync(_service.LogLevel, cancellationToken);

                _scannedIoLinkDevices = [.. scanResult.Devices
                                                      .Where(d => !d.IsUnreachable)
                                                      // Todo: Vergleich zuverlässiger machen
                                                      .Where(d => !currentIoLinkMasters.Any(m => m.Url == d.Address))
                                                      .OrderBy(d => d.Address.ToString())];

                _scanIoLinkErrors = scanResult.Messages;

                await InvokeAsync(StateHasChanged);
            }
            catch (Exception ex)
            {
                _scanIoLinkErrors.Add($"An error occured while trying to scan for IO-Link Masters: {ex.GetType()}: {ex.Message}");
            }
        }, cancellationToken);
    }

    private void SetAllDatapointsCompression(PoolingGrid poolingGrid)
    {
        if (_tree is null)
            return;

        lock (_treeLock)
        {
            var compressorConfigurations = _tree.GetNodeAndDescendants()
                                         .OfType<IDeviceTreeCompressableDataNode>()
                                         .SelectMany(n => n.CompressorConfigurations);

            foreach (var compressorConfiguration in compressorConfigurations)
            {
                compressorConfiguration.CompressionTime = (int)poolingGrid;
            }

            _changedMasterDevices.Clear();
            _changedMasterDevices.AddRange(_tree.GetNodeAndDescendants()
                                            .OfType<IDeviceTreeMasterNode>());
        }

        _saveReasons = string.Empty;

        _service.DeviceTreeChanged = true;
    }

    private void SetAllDatapointsEnabled(bool enabled)
    {
        var dataNodes = _service.FilteredGridItems.Select(i => i.DataNode).ToArray();

        var compressorConfigurations = dataNodes.OfType<IDeviceTreeCompressableDataNode>()
                                                .SelectMany(n => n.CompressorConfigurations);

        foreach (var compressorConfiguration in compressorConfigurations)
        {
            compressorConfiguration.Enabled = enabled;
        }


        var nonMoneoConnections = _publishTargets.Except(new MoneoCloudFilter().GetCloudConnections(_publishTargets));

        var schedulerConfigurations = dataNodes.OfType<IDeviceTreeSchedulableDataNode>()
                                               .SelectMany(n => n.SchedulerConfigurations)
                                               .Where(s => nonMoneoConnections.Any(c => c.Id == s.DataGroupIdentifier));

        foreach (var schedulerConfiguration in schedulerConfigurations)
        {
            schedulerConfiguration.Enabled = enabled;
        }

        _changedMasterDevices.AddRange(dataNodes.OfType<IDeviceTreeMasterNode>());
        _saveReasons += " data points enabled via debug;";

        CheckDataPointRecommendedLimit(true);
        _service.DeviceTreeChanged = true;
    }

    public void SetDebugRawDataGrid()
        => _rawDataPullingMaxTimesADay = 24 * 60 / 5;

    private void SetGridItems()
    {
        _service.GridItems = [.. _adapter.GetRelevantDataNodes()
            .Where(dn => dn.Visible && dn.DataType.SupportedForLogging())
            .Select(DataNodeToGridModel)
        ];

        ManagementGridRowModel DataNodeToGridModel(IDeviceTreeDataNode dataNode)
            => new()
            {
                DataNode = dataNode,
                PathToNode = _nodePaths![dataNode],
            };
    }

    private void SetNodesIsOffline(string[] nodeIds, bool isOffline)
    {
        if (_tree is null)
            return;

        lock (_treeLock)
        {
            var allNodes = _tree.GetNodeAndDescendants().ToDictionary(n => n.Id, n => n);

            foreach (var nodeId in nodeIds)
            {
                if (allNodes.TryGetValue(nodeId, out var node))
                {
                    node.IsOffline = isOffline;
                }
            }
        }
    }

    private async Task UpdateDeviceTreeAsync(bool retainNewFlags, bool triggerSubscriber)
    {
        try
        {
            IDeviceTreeMasterNode[] devices;
            lock (_treeLock)
            {
                devices = [.. _tree!.Children.OfType<IDeviceTreeMasterNode>()];
            }

            if (devices.Length == 0)
            {
                _displayLoadingSpinner = false;
                await InvokeAsync(StateHasChanged);
                return;
            }

            await DataCollectionWizardService.RequestExistingDevicesAsync(devices, triggerSubscriber, async (receivedDevices) =>
            {
                IDeviceTreeMasterNode[] mastersThatNeedToBeUpdated;
                IDeviceTreeMasterNode[] mastersWithNewNodes;
                IDeviceTreeMasterNode[] mastersWithMissingEngines;
                IDeviceTreeMasterNode[] mastersWithNewUnits;

                lock (_treeLock)
                {

                    var oldStructureUnits = _tree.GetNodeAndDescendants().OfType<DeviceTreeProcessData>().ToDictionary(n => n.Id, n => n.StructureUnit);

                    DeviceTreeBuilder.ExtendCurrentDeviceTree(_tree, [.. receivedDevices.Select(d => d.device).Where(d => d is not null && !d.IsOffline).Cast<IDeviceTreeBase>()], _publishTargets, retainNewFlags);

                    _tree.Name = Localization.DataCollectionWizardPage.Devices;
                    SetTreeCore(_tree, true);
                    _adapter.Builder.Expansion.ChangeExpansionForLayers(true, 0, 0);

                    var masterNodes = _tree.GetNodeAndDescendants().OfType<IDeviceTreeMasterNode>().ToArray();
                    var missingEngineDeviceAddresses = receivedDevices.Where(s => !s.success).Select(a => a.address).ToArray();

                    mastersWithNewNodes = [.. masterNodes.Where(n => n.GetNodeAndDescendants().Any(c => c.IsNew))];
                    mastersWithMissingEngines = [.. masterNodes.Where(m => missingEngineDeviceAddresses.Contains(new UriBuilder(m.Url).Uri))];
                    mastersWithNewUnits = [.. masterNodes.Where(m => HasChangedUnits(m, oldStructureUnits))];

                    mastersThatNeedToBeUpdated = [.. mastersWithNewNodes.Union(mastersWithMissingEngines)
                                                                        .Union(mastersWithNewUnits)
                                                                        .DistinctBy(m => m.Id)];

                    foreach (var dataNode in _tree.GetNodeAndDescendants().OfType<IDeviceTreeDataNode>())
                    {
                        dataNode.AddConfigurations(_publishTargets);
                    }
                }

                CheckDataPointRecommendedLimit(true);

                _service.DeviceTreeChanged = mastersThatNeedToBeUpdated.Length > 0 || _deletedNodes.Count > 0 || _service.DeviceTreeChanged;
                _changedMasterDevices.AddRange(mastersThatNeedToBeUpdated);

                if (mastersWithNewNodes.Length != 0)
                    _saveReasons += " new nodes have been added;";

                if (mastersWithMissingEngines.Length != 0)
                    _saveReasons += " some engines appear to be not running;";

                if (mastersWithNewUnits.Length != 0)
                    _saveReasons += " some units appear to have changed;";

                _displayLoadingSpinner = false;
                await InvokeAsync(StateHasChanged);
            });
        }
        catch (Exception ex)
        {
            LogUpdateDeviceTreeError(Logger, ex.GetType().Name, ex.Message, ex.StackTrace ?? string.Empty);
        }
    }

    private static bool IsValidHost(string host)
    {
        if (string.IsNullOrWhiteSpace(host))
            return false;

        if (IPAddress.TryParse(host, out var ip))
            return ip.AddressFamily == AddressFamily.InterNetwork && host.Count(c => c == '.') == 3;

        if (host.All(c => char.IsAsciiDigit(c) || c == '.'))
            return false;

        return Uri.CheckHostName(host) == UriHostNameType.Dns;
    }

    private static string ExtractHost(string input)
    {
        var host = input;

        // Remove scheme (e.g. "http://")
        var schemeEnd = host.IndexOf("://", StringComparison.Ordinal);
        if (schemeEnd >= 0)
            host = host[(schemeEnd + 3)..];

        // Remove path
        var pathStart = host.IndexOf('/', StringComparison.Ordinal);
        if (pathStart >= 0)
            host = host[..pathStart];

        // Remove port (but not from IPv6 addresses)
        if (!host.StartsWith('['))
        {
            var lastColon = host.LastIndexOf(":", StringComparison.Ordinal);
            if (lastColon >= 0)
                host = host[..lastColon];
        }

        return host;
    }
}
