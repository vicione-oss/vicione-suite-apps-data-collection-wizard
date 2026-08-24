using System.Globalization;
using System.Net;
using System.Net.Sockets;
using ClusterManagement.Public.Services;
using DataCollectionWizard.Client.Components.ManagementGrid;
using DataCollectionWizard.Client.Components.ManagementGrid.GridCells;
using DataCollectionWizard.Client.Components.ManagementGrid.Models;
using DataCollectionWizard.Client.Components.ManagementGrid.Services;
using DataCollectionWizard.Client.Components.TreeNodeTemplates;
using DataCollectionWizard.Client.Extensions;
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
using Sdk.Client.Infrastructure;
using Sdk.Client.Modules;
using Sdk.Client.Services;
using Sdk.Connections.Contracts;
using Sdk.MessageBanner.Contracts;
using ViciOne.DeviceTree.Contracts;
using ViciOne.DeviceTree.Contracts.Extensions;
using ViciOne.DeviceTree.Contracts.Scanning;
using ViciOne.Ui.Blazor.Components.Dialog.Components;
using ViciOne.Ui.Blazor.Components.LoadingSpinner.Factories;
using ViciOne.Ui.Blazor.Components.LoadingSpinner.Models;
using ViciOne.Ui.Blazor.Components.TextBox;
using ViciOne.Ui.Localization.Resources;

namespace DataCollectionWizard.Client.Components;

public sealed partial class DataCollectionWizardPage : ModulePageBase<DataCollectionWizardClientModule>, IEventConsumer<DeviceTreeApplicationEvent>
{
    private const string VseDialogHeightManual = "405px";
    private const string IoLinkMasterDialogHeightManual = "565px";
    private const string ScanDialogHeight = "680px";
    private const int MaxRecommendedDataPoints = 100;


    /// <summary>
    /// Longest alias the dialog accepts.
    /// </summary>
    private const int AliasMaximumLength = 64;
    private DeviceTreeAdapter _adapter = default!;
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
            Message = Localization.DataCollectionWizardPage.SpinnerMessageTriggeringDeviceTreeScan,
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
            Message = Localization.DataCollectionWizardPage.SpinnerMessageTriggeringDeviceTreeScan,
        },
        TimedMessageFactory.CreateGap(1),
        new()
        {
            Message = Localization.DataCollectionWizardPage.SpinnerMessageWaitingForScanResult,
        },
    ];
    private readonly TimedMessage[] _loadingSpinnerMessagesNetworkScan =
    [
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
            Message = Localization.DataCollectionWizardPage.SpinnerMessageTriggeringDeviceTreeScan,
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
    private bool _showUnsavedChanges;
    private TaskCompletionSource<UnsavedLeaveChoice>? _unsavedLeaveChoice;

    private List<DcpDevice>? _scannedIoLinkDevices;
    private readonly List<string> _selectedIoLinkDevices = [];
    // Credentials entered for IO-Link masters that require authentication, keyed by the device address string as
    // held in _selectedIoLinkDevices. Applied to each created DeviceTreeIoLinkMaster on confirm.
    private readonly Dictionary<string, (string User, string Password)> _ioLinkCredentials = new(StringComparer.OrdinalIgnoreCase);
    private string _newIoLinkMasterUser = string.Empty;
    private string _newIoLinkMasterPassword = string.Empty;
    // "Same credentials for all" mode for the scan tab (only meaningful when more than one selected master requires
    // authentication): one shared username/password applied to all of them instead of one pair per device.
    private bool _ioLinkUseSharedCredentials;
    private string _ioLinkSharedUser = string.Empty;
    private string _ioLinkSharedPassword = string.Empty;
    private bool _ioLinkShowCredentialStep;
    private List<VseScanDevice>? _scannedVseDevices;
    private readonly List<string> _selectedVseDevices = [];
    private string _vseDialogHeight = VseDialogHeightManual;
    private int _vseTabIndex;
    private string _vseFilter = string.Empty;
    private List<string> _scanVseErrors = [];
    private DeviceTreeRoot? _tree;
    private readonly Lock _treeLock = new();
    private string _ioLinkMasterDialogHeight = IoLinkMasterDialogHeightManual;
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

    /// <summary>
    /// The name of the device the alias dialog is currently editing.
    /// </summary>
    private string AliasSubjectName => _editingNode?.Device.Name ?? string.Empty;

    /// <summary>
    /// A second line identifying that device - its family and address, as far as the node reports them.
    /// </summary>
    private string? AliasSubjectDetail
        => _editingNode?.Device is IDeviceTreeMasterNode master
            ? DeviceTooltipFormat.Join(master.DeviceFamily, DeviceTooltipFormat.Address(master.Url))
            : null;

    /// <summary>
    /// How the node will read in the tree with the alias currently typed - the alias does not replace the
    /// device's name but precedes it, which is not otherwise visible while typing.
    /// </summary>
    private string AliasTreePreviewText
        => DeviceTreeNodeNameProvider.GetUserAliasDisplayText(_deviceAlias, AliasSubjectName);

    private string AliasCharacterCountText
        => string.Format(CultureInfo.CurrentCulture, Localization.DataCollectionWizardPage.AliasCharacterCount,
            _deviceAlias.Length, AliasMaximumLength);

    protected override string PageTitle => Localization.DataCollectionWizardPage.Title;

    [Inject] private IConnectionService ConnectionService { get; set; } = null!;
    [Inject] private IEnumerable<ICloudFilter> CloudFilters { get; set; } = null!;
    [Inject] private IDataCollectionWizardService DataCollectionWizardService { get; set; } = null!;
    [Inject] private IResourceDownloadStateService ResourceDownloadState { get; set; } = null!;
    [Inject] private IMessageBannerService MessageBannerService { get; set; } = default!;
    [Inject] private IUiMediator Mediator { get; set; } = default!;
    [Inject] private DeviceTreeNodeIconProvider IconProvider { get; set; } = default!;

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

        // The count is surfaced by the passive usage meter under the title (see DataCollectionWizardToolbar), which
        // colours itself amber/red near/over the limit - so just re-render; no more pop-up banner.
        _ = InvokeAsync(StateHasChanged);
    }

    // How the user answered the "unsaved changes" dialog when leaving the page.
    private enum UnsavedLeaveChoice
    {
        Cancel,  // stay on the page
        Discard, // leave without saving
        Save,    // save (then leave)
    }

    private async Task ConfirmLeave(LocationChangingContext context)
    {
        if (!_service.DeviceTreeChanged)
            return;

        _unsavedLeaveChoice = new TaskCompletionSource<UnsavedLeaveChoice>();
        _showUnsavedChanges = true;
        StateHasChanged();

        var choice = await _unsavedLeaveChoice.Task;
        _showUnsavedChanges = false;
        StateHasChanged();

        switch (choice)
        {
            case UnsavedLeaveChoice.Cancel:
                context.PreventNavigation();
                break;
            case UnsavedLeaveChoice.Save:
                // Trigger the save just like the toolbar button; the deploy runs in the background and the tree is
                // already marked unchanged, so navigation may proceed.
                SaveButtonAsync();
                break;
            case UnsavedLeaveChoice.Discard:
                break; // let the navigation proceed
        }
    }

    private void ResolveUnsavedLeave(UnsavedLeaveChoice choice)
        => _unsavedLeaveChoice?.TrySetResult(choice);

    private void FillPublishTargets()
    {
        _publishTargets.Clear();
        _publishTargets.AddRange(PublishTargetsFilter.GetPublishTargets(ConnectionService.Connections, CloudFilters));

        _publishTargetInfos = [.. _publishTargets.Select(c =>
        {
            var cloudFilter = CloudFilters.FirstOrDefault(f => f.GetCloudConnections([c]).Any());
            if (cloudFilter is null)
            {
                return new PublishTargetInfo(c, ConnectionKind.Unsupported, []);
            }

            return new PublishTargetInfo(c, cloudFilter.ConnectionKind, cloudFilter.TreeNodesSupportedForConfiguration);
        })];

        // Share the clouds with the service so the sidebar info panel can project the throughput per cloud, and
        // recompute once now that the target list is known.
        _service.PublishTargets = _publishTargetInfos;
        _service.InvokeConfigChanged();
    }

    private static bool MatchesIoLinkFilter(DcpDevice device, string filter)
        => (device.Network.Address?.ToString()?.Contains(filter, StringComparison.OrdinalIgnoreCase) ?? false)
            || device.Identity.Name.Contains(filter, StringComparison.OrdinalIgnoreCase)
            || device.Network.MacAddress.Contains(filter, StringComparison.OrdinalIgnoreCase)
            || device.Identity.VendorId.ToString(CultureInfo.InvariantCulture).Contains(filter, StringComparison.OrdinalIgnoreCase)
            || device.Identity.DeviceId.ToString(CultureInfo.InvariantCulture).Contains(filter, StringComparison.OrdinalIgnoreCase);

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

    private static bool HasChangedUnits(IDeviceTreeMasterNode masterNode, Dictionary<string, string> oldStructureUnits)
    {
        foreach (var processData in masterNode.GetNodeAndDescendants().OfType<DeviceTreeProcessData>())
        {
            if (oldStructureUnits.TryGetValue(processData.Id, out var oldUnit) && oldUnit != processData.Unit)
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
            await DataCollectionWizardService.AddDeviceScannerDataflow(_service.LogLevel);
            await UpdateDeviceTreeAsync(false, false);
        }
        catch (Exception ex)
        {
            LogInitDcwError(Logger, ex);
            await InvokeAsync(() => MessageBannerService.ShowMessageBanner(MessageType.Warning, CommonVocabulary.Error));
        }
    }

    private bool IsIoLinkMasterDialogOkEnabled()
    {
        if (_ioLinkMasterTabIndex == 0)
            return !string.IsNullOrWhiteSpace(_newIoLinkMasterAddress);

        if (_selectedIoLinkDevices.Count == 0)
            return false;

        var authAddresses = SelectedAuthRequiredAddresses();
        // In "same credentials for all" mode one shared username covers every auth-required master; otherwise each
        // selected auth-required master needs its own username entered.
        return _ioLinkUseSharedCredentials && authAddresses.Count > 1
            ? !string.IsNullOrEmpty(_ioLinkSharedUser)
            : authAddresses.All(address => !string.IsNullOrEmpty(GetIoLinkCredentialUser(address)));
    }

    private bool IoLinkScanNeedsCredentialStep()
        => _ioLinkMasterTabIndex == 1 && _selectedIoLinkDevices.Count > 0 && SelectedAuthRequiredAddresses().Count > 0;

    private void GoToIoLinkCredentialStep() => _ioLinkShowCredentialStep = true;

    private void BackToIoLinkSelection() => _ioLinkShowCredentialStep = false;

    /// <summary>
    /// The addresses of the currently selected scanned masters that reported they require authentication.
    /// </summary>
    private List<string> SelectedAuthRequiredAddresses()
        => [.. (_scannedIoLinkDevices ?? [])
            .Where(device => device.Security.RequiresAuthentication &&
                             device.Network.Address is not null &&
                             _selectedIoLinkDevices.Contains(device.Network.Address.ToString()!))
            .Select(device => device.Network.Address!.ToString())];

    private string DeviceNameForAddress(string address)
        => (_scannedIoLinkDevices ?? [])
            .FirstOrDefault(device => string.Equals(device.Network.Address?.ToString(), address, StringComparison.OrdinalIgnoreCase))
            ?.Identity.Name ?? address;

    // The device names repeat across masters, so each credential block is headed by the master's IP instead.
    private string DeviceIpForAddress(string address)
        => (_scannedIoLinkDevices ?? [])
            .FirstOrDefault(device => string.Equals(device.Network.Address?.ToString(), address, StringComparison.OrdinalIgnoreCase))
            ?.Network.Address is { } uri
            ? $"{uri.DnsSafeHost}:{uri.Port}"
            : address;

    private string GetIoLinkCredentialUser(string address)
        => _ioLinkCredentials.TryGetValue(address, out var credential) ? credential.User : string.Empty;

    private string GetIoLinkCredentialPassword(string address)
        => _ioLinkCredentials.TryGetValue(address, out var credential) ? credential.Password : string.Empty;

    private void SetIoLinkCredentialUser(string address, string user)
        => _ioLinkCredentials[address] = (user, GetIoLinkCredentialPassword(address));

    private void SetIoLinkCredentialPassword(string address, string password)
        => _ioLinkCredentials[address] = (GetIoLinkCredentialUser(address), password);

    /// <summary>
    /// Toggles "same credentials for all" mode. When turning it off, the shared credentials are copied into the
    /// per-device fields so nothing entered is lost on the switch back.
    /// </summary>
    private void OnUseSharedCredentialsChanged(bool useShared, List<string> authAddresses)
    {
        _ioLinkUseSharedCredentials = useShared;
        if (useShared)
            return;

        foreach (var address in authAddresses)
        {
            if (!string.IsNullOrEmpty(_ioLinkSharedUser))
                SetIoLinkCredentialUser(address, _ioLinkSharedUser);
            if (!string.IsNullOrEmpty(_ioLinkSharedPassword))
                SetIoLinkCredentialPassword(address, _ioLinkSharedPassword);
        }
    }

    private void SetIoLinkMasterDialogScanHeight(int tabIndex)
    {
        _ioLinkShowCredentialStep = false;
        _ioLinkMasterDialogHeight = tabIndex == 1 ? ScanDialogHeight : IoLinkMasterDialogHeightManual;
        InvokeAsync(StateHasChanged);
    }

    private void SetTree(DeviceTreeRoot root, bool expandToOfflineNodes)
    {
        root.Name = CommonVocabulary.DevicePlural;

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
        _service.HasOfflineNodes = _tree.GetNodeAndDescendants().Any(n => n.Status != ConnectionStatus.Online && n is not IDeviceTreeMasterNode);

        foreach (var dataNode in treeNodes.OfType<IDeviceTreeDataNode>())
            dataNode.AddConfigurations(_publishTargets);

        // Give the info panel the whole tree so its throughput projection covers every device (not just the
        // selected one), and trigger a recompute now that the per-cloud configs are in place.
        _service.TreeRoot = _tree;
        _service.InvokeConfigChanged();
    }

    private bool TryGetExistingDeviceTreeMaster(IDeviceTreeMasterNode device, out IDeviceTreeMasterNode? existingDevice)
        => TryGetExistingDeviceTreeMaster(device, _tree!.Children.OfType<IDeviceTreeMasterNode>(), out existingDevice);

    private static bool TryGetExistingDeviceTreeMaster(IDeviceTreeMasterNode device, IEnumerable<IDeviceTreeMasterNode> existingMasterNodes, out IDeviceTreeMasterNode? existingDevice)
    {
        existingDevice = existingMasterNodes.FirstOrDefault(v => device.Url == v.Url);
        return existingDevice is not null;
    }

    private async Task NodesOffline(string[] nodeIds)
    {
        if (_tree is null)
            return;

        SetNodesStatus(nodeIds, ConnectionStatus.Offline);
        RefreshNodeStatuses(nodeIds);
        await InvokeAsync(StateHasChanged);
    }

    private async Task NodesOnline(string[] nodeIds)
    {
        if (_tree is null)
            return;

        SetNodesStatus(nodeIds, ConnectionStatus.Online);
        RefreshNodeStatuses(nodeIds);
        await InvokeAsync(StateHasChanged);
    }

    // Apply a live status change to the tree without a full rebuild: refresh only the affected nodes' brackets (and
    // their ancestors, so collapsed parents still show the status) and update the offline-nodes flag. Previously these
    // notifications called SetTree, which rebuilt the whole tree + grid on every status change and caused the grid to
    // flicker.
    private void RefreshNodeStatuses(string[] nodeIds)
    {
        _adapter.UpdateNodeStatuses(nodeIds);

        lock (_treeLock)
        {
            _service.HasOfflineNodes = _tree!.GetNodeAndDescendants()
                .Any(n => n.Status != ConnectionStatus.Online && n is not IDeviceTreeMasterNode);
        }
    }

    private async Task OnAddVSEDialogCloseAsync()
    {
        CancelDcwScan();
        await _refAddVSEDialog!.CloseAsync();
    }

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
        if (node.Device is not IDeviceTreeUserAliasNode aliasNode)
            return;

        _deviceAlias = aliasNode.Alias ?? string.Empty;
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
    {
        _vseTabIndex = 0;
        _dcwScanTokenSource = new CancellationTokenSource();
        ScanVseDevices(_dcwScanTokenSource.Token);

        await _refAddVSEDialog!.ShowAsync();
    }


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
            if (!string.IsNullOrEmpty(_newIoLinkMasterUser))
                _ioLinkCredentials[_newIoLinkMasterAddress] = (_newIoLinkMasterUser, _newIoLinkMasterPassword);
        }

        // Resolve the entered credentials to the normalized address the node callback receives, so each created
        // master can be matched to its credentials there. In "same credentials for all" mode the shared pair
        // covers every auth-required selected master.
        var resolvedCredentials = new Dictionary<string, (string User, string Password)>(StringComparer.OrdinalIgnoreCase);
        var authAddresses = SelectedAuthRequiredAddresses();
        var useSharedCredentials = _ioLinkUseSharedCredentials && authAddresses.Count > 1;
        var authAddressSet = new HashSet<string>(authAddresses, StringComparer.OrdinalIgnoreCase);

        foreach (var selectedAddress in _selectedIoLinkDevices)
        {
            (string User, string Password) credential;
            if (useSharedCredentials && authAddressSet.Contains(selectedAddress))
                credential = (_ioLinkSharedUser, _ioLinkSharedPassword);
            else if (!_ioLinkCredentials.TryGetValue(selectedAddress, out credential))
                continue;

            if (!string.IsNullOrEmpty(credential.User))
                resolvedCredentials[new UriBuilder(selectedAddress).Uri.AbsoluteUri] = credential;
        }

        _loadingSpinnerMessages = _loadingSpinnerMessagesScanDevice;
        _displayLoadingSpinner = true;
        await _refAddIoLinkMasterDialog!.CloseAsync();
        var numberOfReceivedDevices = 0;

        var deviceEngineInfos = _selectedIoLinkDevices.Select(d =>
        {
            var uri = new UriBuilder(d).Uri;
            resolvedCredentials.TryGetValue(uri.AbsoluteUri, out var credential);
            return new DeviceEngineInfo(uri, typeof(DeviceTreeIoLinkMaster).AssemblyQualifiedName!, credential.User, credential.Password);
        });

        await DataCollectionWizardService.RequestNewDevicesDeviceTreeAsync(
            deviceEngineInfos,
            async (d, a, _) =>
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
                        MacAddress = "ff:ff:ff:ff:ff",
                        Name = "IO-Link Master",
                        Status = ConnectionStatus.Offline,
                        Url = uri,
                    };
                }

                if (d is DeviceTreeIoLinkMaster masterNode && resolvedCredentials.TryGetValue(uri.AbsoluteUri, out var credential))
                {
                    masterNode.Username = credential.User;
                    masterNode.Password = credential.Password;
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
        CancelDcwScan();

        if (_vseTabIndex == 1)
        {
            await AddScannedVseDevicesAsync();
            return;
        }

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
                    MacAddress = "ff:ff:ff:ff:ff",
                    Name = "VSE Device",
                    Status = ConnectionStatus.Offline,
                    Url = VseAddresses.GetVseAddressWithPort(vseAddress.DnsSafeHost),
                };
            }

            await AddDeviceToDeviceTree(d);
            _displayLoadingSpinner = false;
            await InvokeAsync(StateHasChanged);
        }, true, _service.LogLevel);
    }

    /// <summary>
    /// Adds every VSE device ticked on the dialog's scan tab, the same way the IO-Link dialog adds its
    /// selection.
    /// </summary>
    private async Task AddScannedVseDevicesAsync()
    {
        _loadingSpinnerMessages = _loadingSpinnerMessagesScanDevice;
        _displayLoadingSpinner = true;
        await _refAddVSEDialog!.CloseAsync();

        var numberOfReceivedDevices = 0;
        var deviceEngineInfos = _selectedVseDevices.Select(
            d => new DeviceEngineInfo(VseAddresses.GetVseAddressWithPort(d), typeof(DeviceTreeVseDevice).AssemblyQualifiedName!));

        await DataCollectionWizardService.RequestNewDevicesDeviceTreeAsync(
            deviceEngineInfos,
            async (d, a, _) =>
            {
                if (d is not DeviceTreeVseDevice)
                {
                    LogUnexpectedNullDeviceError(Logger, a.DnsSafeHost);
                    d = new DeviceTreeVseDevice
                    {
                        Description = new DeviceTreeNodeDescription
                        {
                            Text = "an ifm VSE device",
                        },
                        Id = $"vse@{a}",
                        MacAddress = "ff:ff:ff:ff:ff",
                        Name = "VSE Device",
                        Status = ConnectionStatus.Offline,
                        Url = VseAddresses.GetVseAddressWithPort(a.DnsSafeHost),
                    };
                }

                await AddDeviceToDeviceTree(d);
                numberOfReceivedDevices++;

                if (numberOfReceivedDevices >= _selectedVseDevices.Count)
                {
                    _displayLoadingSpinner = false;
                    await InvokeAsync(StateHasChanged);
                }
            },
            true,
            _service.LogLevel);
    }

    private async Task OnAliasDialogCloseAsync()
        => await _refAliasDialog!.CloseAsync();

    private async Task OnAliasDialogOkAsync()
    {
        await _refAliasDialog!.CloseAsync();

        if (_editingNode!.Device is not IDeviceTreeUserAliasNode aliasNode)
            return;

        if (_deviceAlias == aliasNode.Alias)
            return;

        aliasNode.Alias = string.IsNullOrWhiteSpace(_deviceAlias) ? null : _deviceAlias.Trim();
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
                LogAwaitingDeploymentWarning(Logger, ex);
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

        // The node was removed from the tree in place (RemoveNodeFromParent), so TreeRoot is not otherwise
        // re-assigned. Re-assign it to bump TreeVersion (invalidates the info panel's node cache) and raise
        // ConfigChanged so cache-by-version consumers - notably the info panel - recompute without the deleted node.
        _service.TreeRoot = _tree!;
        _service.InvokeConfigChanged();

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
        _adapter = new DeviceTreeAdapter(false, IconProvider);

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
        var deviceAddress = device.Network.Address?.ToString();

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
        => _ = InvokeAsync(() =>
        {
            // Selecting a different tree node shows a different set of rows, so clear the multi-select (and with it
            // the bulk bar). Otherwise rows ticked on the previous node stay selected but invisible - easy to forget.
            _service.ClearSelection();
            _service.GridItems = [];
            _gridNeedsRebuild = true;
            StateHasChanged();
        });

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
            LogRebrowseButtonError(Logger, ex);
        }
    }

    private static IDeviceTreeBase[] RemoveOfflineNodesRecursively(IDeviceTreeBase tree)
    {
        var result = new List<IDeviceTreeBase>();

        for (var i = 0; i < tree.Children.Count; i++)
        {
            var child = tree.Children[i];

            if (child.Status != ConnectionStatus.Online && child is not IDeviceTreeMasterNode)
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

    private void OnAddIoLinkMasterAddressChanged(string value)
    {
        _newIoLinkMasterAddress = value;
        _isIoLinkMasterUriValid = true;
        _isIoLinkMasterUriUnique = true;
    }

    private void ResetAddIoLinkMasterDialog()
    {
        _ioLinkMasterFilter = string.Empty;
        _newIoLinkMasterAddress = string.Empty;
        _isIoLinkMasterUriUnique = true;
        _isIoLinkMasterUriValid = true;
        _ioLinkMasterDialogHeight = IoLinkMasterDialogHeightManual;
        _ioLinkCredentials.Clear();
        _newIoLinkMasterUser = string.Empty;
        _newIoLinkMasterPassword = string.Empty;
        _ioLinkUseSharedCredentials = false;
        _ioLinkSharedUser = string.Empty;
        _ioLinkSharedPassword = string.Empty;
        _ioLinkShowCredentialStep = false;
    }

    private void OnAddVSEAddressChanged(string value)
    {
        _newVSEAddress = value;
        _isVSEUriValid = true;
        _isVSEUriUnique = true;
    }

    private void ResetAddVSEDialog()
    {
        _newVSEAddress = string.Empty;
        _isVSEUriUnique = true;
        _isVSEUriValid = true;
        _vseFilter = string.Empty;
        _vseDialogHeight = VseDialogHeightManual;
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
                            _tree.Name = CommonVocabulary.DevicePlural;
                            SetTreeCore(_tree, false);
                        }
                    }

                    CheckDataPointRecommendedLimit(true);

                    await DataCollectionWizardService.SaveDeviceTreeAsync(changedMasters, deletedNodes, _tree!, _service.LogLevel);
                }
                catch (Exception ex)
                {
                    LogWhileSaveDeviceTreeError(Logger, ex);
                }
            });
        }
        catch (Exception ex)
        {
            LogWhileSaveDeviceTreeError(Logger, ex);
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
                                                      // Match by host, not the full URL: an already-added master stores its
                                                      // URL with the scheme/port it is actually reached on (e.g. https:443
                                                      // after an authenticated master upgrades from the scanned http:80), so
                                                      // a full-URL compare would offer it again as if it were new.
                                                      .Where(d => !currentIoLinkMasters.Any(m =>
                                                          string.Equals(m.Url.DnsSafeHost, d.Network.Address?.DnsSafeHost, StringComparison.OrdinalIgnoreCase)))
                                                      .OrderBy(d => d.Network.Address.ToString())];

                _scanIoLinkErrors = scanResult.Messages;

                await InvokeAsync(StateHasChanged);
            }
            catch (Exception ex)
            {
                _scanIoLinkErrors.Add($"An error occurred while trying to scan for IO-Link Masters: {ex.GetType()}: {ex.Message}");
            }
        }, cancellationToken);
    }

    private void ScanVseDevices(CancellationToken cancellationToken)
    {
        _scannedVseDevices = null;
        _scanVseErrors = [];
        _selectedVseDevices.Clear();

        DeviceTreeVseDevice[] currentVseDevices;
        lock (_treeLock)
        {
            currentVseDevices = [.. _tree!.GetNodeAndDescendants().OfType<DeviceTreeVseDevice>()];
        }

        _ = Task.Run(async () =>
        {
            try
            {
                var scanResult = await DataCollectionWizardService.ScanVseDevicesAsync(_service.LogLevel, cancellationToken);

                _scannedVseDevices = [.. scanResult.Devices
                                                   .Where(d => !currentVseDevices.Any(v => v.Url == VseScanDeviceAddress(d)))
                                                   .OrderBy(d => d.IpAddress, StringComparer.Ordinal)];

                _scanVseErrors = scanResult.Messages;

                await InvokeAsync(StateHasChanged);
            }
            catch (Exception ex)
            {
                _scanVseErrors.Add($"An error occurred while trying to scan for VSE devices: {ex.GetType()}: {ex.Message}");
            }
        }, cancellationToken);
    }

    /// <summary>
    /// The address a scanned VSE device is reached at. The scan reports host and port separately, while the
    /// device tree identifies a VSE by the same URL the manual entry produces.
    /// </summary>
    private static Uri VseScanDeviceAddress(VseScanDevice device)
        => VseAddresses.GetVseAddressWithPort(device.IpAddress);

    private static bool MatchesVseFilter(VseScanDevice device, string filter)
        => device.IpAddress.Contains(filter, StringComparison.OrdinalIgnoreCase)
            || device.HostName.Contains(filter, StringComparison.OrdinalIgnoreCase)
            || device.MacAddress.Contains(filter, StringComparison.OrdinalIgnoreCase)
            || device.DeviceType.Contains(filter, StringComparison.OrdinalIgnoreCase);

    private void OnRescanVseDevicesClicked()
        => ScanVseDevices(_dcwScanTokenSource.Token);

    private void OnScannedVseDeviceSelectionChanged(bool selected, VseScanDevice device)
    {
        if (string.IsNullOrWhiteSpace(device.IpAddress))
        {
            LogMissingAddressSelectedWarning(Logger);
            return;
        }

        if (selected)
            _selectedVseDevices.Add(device.IpAddress);
        else
            _selectedVseDevices.Remove(device.IpAddress);
    }

    private bool IsVseDialogOkEnabled()
        => _vseTabIndex == 0
            ? !string.IsNullOrWhiteSpace(_newVSEAddress)
            : _selectedVseDevices.Count > 0;

    private void SetVseDialogScanHeight(int tabIndex)
    {
        _vseDialogHeight = tabIndex == 1 ? ScanDialogHeight : VseDialogHeightManual;
        InvokeAsync(StateHasChanged);
    }

    private void SetAllDatapointsCompression(AggregationInterval aggregationInterval)
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
                compressorConfiguration.CompressionTime = (int)aggregationInterval;
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

    // Multi-select reset: puts the selection's settings back to what a freshly discovered data point is given.
    // The values themselves live in DeviceTreeDataNodeExtensions, next to the code that creates configurations in
    // the first place, so the two cannot drift apart.
    private void OnBulkResetSelection(BulkResetRequest request)
    {
        if (_tree is null)
            return;

        var connections = (request.Target is null
                ? _publishTargets.Where(connection => IsConfigurable(connection))
                : [request.Target.Connection])
            .ToList();

        if (connections.Count == 0)
            return;

        var selectedNodes = _service.SelectedNodes;

        // Collected while writing, so the grid can point out the cells this reached.
        var changedNodes = new HashSet<IDeviceTreeDataNode>();

        lock (_treeLock)
        {
            foreach (var node in selectedNodes)
            {
                if (node.ResetConfigurations(connections))
                    changedNodes.Add(node);
            }

            _changedMasterDevices.Clear();
            _changedMasterDevices.AddRange(_tree.GetNodeAndDescendants().OfType<IDeviceTreeMasterNode>());
        }

        _saveReasons += " data point settings reset via multi-select;";

        CheckDataPointRecommendedLimit(true);
        _service.DeviceTreeChanged = true;
        _service.InvokeBulkEnableApplied(
            new BulkChangeHighlight(changedNodes, connections.Select(connection => connection.Id).ToHashSet()));
    }

    // Multi-select bulk enable/disable: applies to the process-value data points in the grid selection for the
    // chosen publish target (or all targets). RawData/event-triggered recordings are intentionally left out for
    // now - they carry more than one toggle, so they need their own bulk action.
    private void OnBulkEnableSelection(BulkEnableRequest request)
    {
        if (_tree is null)
            return;

        var targetConnections = request.Target is null
            ? (IEnumerable<Connection>)_publishTargets
            : [request.Target.Connection];
        var connectionIds = targetConnections.Select(connection => connection.Id).ToHashSet();

        var selectedNodes = _service.SelectedNodes;

        // Collected while writing, so the grid can point out exactly the cells this reached.
        var changedNodes = new HashSet<IDeviceTreeDataNode>();

        lock (_treeLock)
        {
            foreach (var node in selectedNodes.OfType<IDeviceTreeCompressableDataNode>())
            {
                foreach (var configuration in node.CompressorConfigurations
                             .Where(configuration => connectionIds.Contains(configuration.DataGroupIdentifier)))
                {
                    configuration.Enabled = request.Enabled;
                    changedNodes.Add(node);
                }
            }

            var nonMoneoConnectionIds = _publishTargets
                .Except(new MoneoCloudFilter().GetCloudConnections(_publishTargets))
                .Select(connection => connection.Id)
                .ToHashSet();

            foreach (var node in selectedNodes.OfType<IDeviceTreeSchedulableDataNode>())
            {
                foreach (var configuration in node.SchedulerConfigurations
                             .Where(configuration => connectionIds.Contains(configuration.DataGroupIdentifier)
                                                     && nonMoneoConnectionIds.Contains(configuration.DataGroupIdentifier)))
                {
                    configuration.Enabled = request.Enabled;
                    changedNodes.Add(node);
                }
            }

            _changedMasterDevices.Clear();
            _changedMasterDevices.AddRange(_tree.GetNodeAndDescendants().OfType<IDeviceTreeMasterNode>());
        }

        _saveReasons += " data points enabled via multi-select;";

        CheckDataPointRecommendedLimit(true);
        _service.DeviceTreeChanged = true;
        _service.InvokeBulkEnableApplied(new BulkChangeHighlight(changedNodes, connectionIds));
    }

    // Multi-select bulk settings: writes one setting on the grid selection for the chosen publish target (or all
    // configurable ones). Enabling stays in OnBulkEnableSelection; this handles everything the panel adds.
    private void OnBulkSettingSelection(BulkSettingRequest request)
    {
        if (_tree is null)
            return;

        var targetIds = (request.Target is null
                ? _publishTargets.Where(connection => IsConfigurable(connection))
                : [request.Target.Connection])
            .Select(connection => connection.Id)
            .ToHashSet();

        if (targetIds.Count == 0)
            return;

        var selectedNodes = _service.SelectedNodes;

        // Collected while writing, so the grid can point out exactly the cells this reached. A setting only
        // applies to the data types that support it, so this is usually a subset of the selection.
        var changedNodes = new HashSet<IDeviceTreeDataNode>();

        lock (_treeLock)
        {
            switch (request.Setting)
            {
                case BulkSetting.ProcessEnabled:
                case BulkSetting.UncompressedEnabled:
                    foreach (var (node, configuration) in CompressorConfigurations(request.Setting == BulkSetting.ProcessEnabled))
                    {
                        configuration.Enabled = (bool)request.Value;
                        changedNodes.Add(node);
                    }
                    break;

                case BulkSetting.ProcessAggregationInterval:
                    foreach (var (node, configuration) in CompressorConfigurations(compressible: true))
                    {
                        configuration.CompressionTime = (int)(AggregationInterval)request.Value;
                        changedNodes.Add(node);
                    }
                    break;

                case BulkSetting.ProcessAggregationFunction:
                    // "On Change" locks the function in the single-row editor, so a bulk change must leave those
                    // configurations alone rather than writing a value that could not be set there.
                    foreach (var (node, configuration) in CompressorConfigurations(compressible: true)
                                 .Where(entry => entry.Configuration.CompressionTime != (int)AggregationInterval.OnChange))
                    {
                        configuration.Aggregation = (AggregationFunction)request.Value;
                        changedNodes.Add(node);
                    }
                    break;

                case BulkSetting.RecordingEnabled:
                    foreach (var (node, configuration) in SchedulerConfigurations())
                    {
                        configuration.Enabled = (bool)request.Value;
                        changedNodes.Add(node);
                    }
                    break;

                case BulkSetting.RecordingDays:
                    foreach (var (node, configuration) in SchedulerConfigurations().Where(entry => entry.Configuration.Times.Count > 0))
                    {
                        Reschedule(configuration, configuration.Times.First().Value.Length, ((DaysOfWeek)request.Value).AsEnumerable());
                        changedNodes.Add(node);
                    }
                    break;

                case BulkSetting.RecordingTimesADay:
                    foreach (var (node, configuration) in SchedulerConfigurations().Where(entry => entry.Configuration.Times.Count > 0))
                    {
                        Reschedule(configuration, (int)request.Value, [.. configuration.Times.Keys]);
                        changedNodes.Add(node);
                    }
                    break;

                // Enabled is written on its own: it is the switch that turns a trigger off without losing how it
                // was set up, so enabling one restores exactly what was configured there.
                case BulkSetting.TriggerEnabled:
                    foreach (var (node, trigger) in Triggers())
                    {
                        trigger.Enabled = (bool)request.Value;
                        changedNodes.Add(node);
                    }
                    break;

                case BulkSetting.TriggerOnDamage:
                    foreach (var (node, trigger) in Triggers())
                    {
                        trigger.OnDamage = (bool)request.Value;
                        changedNodes.Add(node);
                    }
                    break;

                case BulkSetting.TriggerOnWarning:
                    foreach (var (node, trigger) in Triggers())
                    {
                        trigger.OnWarning = (bool)request.Value;
                        changedNodes.Add(node);
                    }
                    break;

                case BulkSetting.TriggerDelay:
                    foreach (var (node, trigger) in Triggers())
                    {
                        trigger.Delay = (int)request.Value;
                        changedNodes.Add(node);
                    }
                    break;

                case BulkSetting.RawDataFrequency:
                    foreach (var (node, settings) in RawDataSettings())
                    {
                        settings.Frequency = (int)request.Value;
                        changedNodes.Add(node);
                    }
                    break;

                case BulkSetting.RawDataDuration:
                    foreach (var (node, settings) in RawDataSettings())
                    {
                        settings.Duration = (int)request.Value;
                        changedNodes.Add(node);
                    }
                    break;

                default:
                    return;
            }

            _changedMasterDevices.Clear();
            _changedMasterDevices.AddRange(_tree.GetNodeAndDescendants().OfType<IDeviceTreeMasterNode>());
        }

        _saveReasons += " data point settings changed via multi-select;";

        CheckDataPointRecommendedLimit(true);
        _service.DeviceTreeChanged = true;
        _service.InvokeBulkEnableApplied(new BulkChangeHighlight(changedNodes, targetIds));

        // Each of these yields the owning node alongside the configuration, so the caller can record which rows a
        // change actually reached without walking the selection a second time.
        IEnumerable<(IDeviceTreeDataNode Node, CompressorConfiguration Configuration)> CompressorConfigurations(bool compressible)
            => selectedNodes.OfType<IDeviceTreeCompressableDataNode>()
                .Where(node => node.DataType.SupportsLogging && node.DataType.SupportsCompression == compressible)
                .SelectMany(node => node.CompressorConfigurations
                    .Where(configuration => targetIds.Contains(configuration.DataGroupIdentifier))
                    .Select(configuration => ((IDeviceTreeDataNode)node, configuration)));

        IEnumerable<(IDeviceTreeDataNode Node, SchedulerConfiguration Configuration)> SchedulerConfigurations()
            => selectedNodes.OfType<IDeviceTreeSchedulableDataNode>()
                .SelectMany(node => node.SchedulerConfigurations
                    .Where(configuration => targetIds.Contains(configuration.DataGroupIdentifier))
                    .Select(configuration => ((IDeviceTreeDataNode)node, configuration)));

        // One selected row carries a trigger per sensor and per cloud, so a change reaches more configurations
        // than the selection has rows.
        IEnumerable<(IDeviceTreeDataNode Node, EventTrigger Trigger)> Triggers()
            => selectedNodes.OfType<IDeviceTreeEventTriggerDataNode>()
                .SelectMany(node => node.EventTriggerConfigurations
                    .SelectMany(sensor => sensor.Triggers)
                    .Where(trigger => targetIds.Contains(trigger.DataGroupIdentifier))
                    .Select(trigger => ((IDeviceTreeDataNode)node, trigger)));

        IEnumerable<(IDeviceTreeDataNode Node, RawDataSettings Settings)> RawDataSettings()
            => selectedNodes.OfType<IDeviceTreeConfigurableRawDataNode>()
                .SelectMany(node => node.RawDataConfigurations
                    .Where(entry => targetIds.Contains(entry.Key))
                    .Select(entry => ((IDeviceTreeDataNode)node, entry.Value)));

        // Rebuilds the schedule the same way the single-row editor does, so both produce identical Times.
        static void Reschedule(SchedulerConfiguration configuration, int timesADay, IEnumerable<DayOfWeek> days)
        {
            var scheduling = BlobDataCell.GetNewScheduling(timesADay, days);
            configuration.Times.Clear();

            foreach (var entry in scheduling)
                configuration.Times[entry.Key] = entry.Value;
        }
    }

    // A publish target whose cloud filter supports no node type at all cannot be configured (moneo today), so a
    // bulk change scoped to "all clouds" must skip it instead of writing settings the grid would not let you set.
    private bool IsConfigurable(Connection connection)
        => _publishTargetInfos.FirstOrDefault(info => info.Connection.Id == connection.Id)
            is { TreeNodesSupportedForConfiguration.Count: > 0 };

    public void SetDebugRawDataGrid()
        => _rawDataPullingMaxTimesADay = 24 * 60 / 5;

    private void SetGridItems()
    {
        var nodePaths = _nodePaths;
        if (nodePaths is null)
            return;

        _service.GridItems = [.. _adapter.GetRelevantDataNodes()
            .Where(dn => dn is not IDeviceTreeHiddenNode && dn.DataType.SupportsLogging && nodePaths!.ContainsKey(dn))
            .Select(DataNodeToGridModel)
        ];

        ManagementGridRowModel DataNodeToGridModel(IDeviceTreeDataNode dataNode)
            => new()
            {
                DataNode = dataNode,
                PathToNode = nodePaths![dataNode],
            };
    }

    private void SetNodesStatus(string[] nodeIds, ConnectionStatus status)
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
                    node.Status = status;
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

                    var oldStructureUnits = _tree.GetNodeAndDescendants().OfType<DeviceTreeProcessData>().ToDictionary(n => n.Id, n => n.Unit);
                    var freshOrUnchangedDevices = receivedDevices
                        .Select(r =>
                        {
                            // A scan result that confirms the master is online replaces the persisted node outright.
                            if (r.device is not null && r.device.Status == ConnectionStatus.Online)
                                return (IDeviceTreeBase?)r.device;

                            // Otherwise the scan did not confirm it online (the engine reported it offline, or the
                            // request timed out / no data arrived): keep the last known structure but mark it and its
                            // children offline, instead of leaving a stale "online".
                            var persisted = devices.FirstOrDefault(m => m.Url == r.address) as IDeviceTreeBase;
                            if (persisted is not null)
                            {
                                foreach (var node in persisted.GetNodeAndDescendants())
                                    node.Status = ConnectionStatus.Offline;
                            }
                            return persisted;
                        })
                        .Where(d => d is not null)
                        .Cast<IDeviceTreeBase>();

                    DeviceTreeBuilder.ExtendCurrentDeviceTree(_tree, [.. freshOrUnchangedDevices], _publishTargets, retainNewFlags);

                    _tree.Name = CommonVocabulary.DevicePlural;
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
            LogUpdateDeviceTreeError(Logger, ex);
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
            var lastColon = host.LastIndexOf(':');
            if (lastColon >= 0)
                host = host[..lastColon];
        }

        return host;
    }
}
