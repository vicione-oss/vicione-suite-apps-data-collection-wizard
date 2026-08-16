using System.Text.Json;
using ClusterManagement.Public.DataflowEvents;
using ClusterManagement.Public.Events;
using ClusterManagement.Public.Requests;
using DataCollectionWizard.Internal;
using DataCollectionWizard.Internal.Commands;
using DataCollectionWizard.Internal.Contracts;
using DataCollectionWizard.Internal.Events;
using DataCollectionWizard.Internal.Requests;
using DataCollectionWizard.Internal.Services;
using DataCollectionWizard.Internal.Services.DesignIds;
using DataCollectionWizard.Public.Events;
using DataCollectionWizard.Public.Extensions;
using DataCollectionWizard.Public.Requests;
using Microsoft.Extensions.Logging;
using Sdk.Client.Infrastructure;
using Sdk.Messaging;
using Sdk.Utils;
using ViciOne.DeviceTree.Contracts;
using ViciOne.DeviceTree.Contracts.Extensions;
using ViciOne.DeviceTree.Contracts.Scanning;

namespace DataCollectionWizard.Client.Services;

public sealed partial class DataCollectionWizardService : IDataCollectionWizardService, IEventConsumer<DeviceTreeEngineAddedEvent>, IEventConsumer<DeviceScannerEngineAddedEvent>,
                                                          IEventConsumer<DeviceConnectorIdsChangedEvent>,
                                                          IEventConsumer<ClusterUpdateCompleted>, IEventConsumer<ClusterUpdateChanged>, IEventConsumer<DeviceTreeChangedEvent>,
                                                          IEventConsumer<ClusterUpdateFailed>, IEventConsumer<ClusterUpdateRejected>,
                                                          IEventConsumer<ClusterUpdateStarted>, IEventConsumer<NodesOnlineEvent>, IEventConsumer<NodesOfflineEvent>
{
    private const int FrontendDeviceTimeout = 40000;
    private const int DeviceScanTimeout = 60000;
    private const int MaxTimeout = FrontendDeviceTimeout + DeviceScanTimeout;

    private readonly IClusterService _clusterService;
    private readonly IEventBroker _eventBroker;
    private readonly Dictionary<Uri, (Guid deviceTreeTrigger, Guid deviceTreeOutput)> _deviceTreeConnectors = [];
    private readonly IUiMediator _mediator;
    private readonly ILogger<DataCollectionWizardService> _logger;
    private readonly Dictionary<Guid, List<(Func<IDeviceTreeMasterNode?, Uri, bool, Task> callback, Type deviceType, Uri address)>> _newDeviceEngineRequests = [];
    private readonly SemaphoreSlim _requestDevicesSemaphore = new(1, 1);
    private readonly AutoDisposeList<IDisposable> _autoDisposeList = [];
    private readonly SemaphoreSlim _deviceScannerEngineAddedSemaphore = new(0, 1);
    private readonly SemaphoreSlim _deploymentResetEvent = new(0, 1);
    private bool _deploymentInProgress;
    private bool _skipNextDeviceTreeChange;

    public event Action<bool>? DataPossiblyInvalid;
    public event Func<string[], Task>? NodesOffline;
    public event Func<string[], Task>? NodesOnline;

    public DataCollectionWizardService(IUiMediator mediator, IClusterService clusterService,
        IEventBroker eventBroker, ILogger<DataCollectionWizardService> logger)
    {
        _clusterService = clusterService;
        _eventBroker = eventBroker;
        _mediator = mediator;
        _logger = logger;

        _autoDisposeList.Add(_mediator.Register<DeviceTreeEngineAddedEvent>(this));
        _autoDisposeList.Add(_mediator.Register<DeviceScannerEngineAddedEvent>(this));
        _autoDisposeList.Add(_mediator.Register<DeviceConnectorIdsChangedEvent>(this));
        _autoDisposeList.Add(_mediator.Register<ClusterUpdateCompleted>(this));
        _autoDisposeList.Add(_mediator.Register<ClusterUpdateChanged>(this));
        _autoDisposeList.Add(_mediator.Register<DeviceTreeChangedEvent>(this));
        _autoDisposeList.Add(_mediator.Register<ClusterUpdateFailed>(this));
        _autoDisposeList.Add(_mediator.Register<ClusterUpdateRejected>(this));
        _autoDisposeList.Add(_mediator.Register<ClusterUpdateStarted>(this));
        _autoDisposeList.Add(_mediator.Register<NodesOfflineEvent>(this));
        _autoDisposeList.Add(_mediator.Register<NodesOnlineEvent>(this));
    }

    public async Task<bool> AddDeviceScannerDataflow(LogLevel logLevel)
    {
        await DrainSemaphoreAsync(_deviceScannerEngineAddedSemaphore);
        await _mediator.Send(new AddDeviceScanner(logLevel));

        if (!await _deviceScannerEngineAddedSemaphore.WaitAsync(25000))
        {
            LogDeviceScanEngineTimeoutCreatingDataflow(_logger);
            return false;
        }

        return true;
    }

    public Task Consume(ClientContext<DeviceScannerEngineAddedEvent> context, CancellationToken cancellationToken)
    {
        SignalSemaphore(_deviceScannerEngineAddedSemaphore);
        return Task.CompletedTask;
    }

    public Task Consume(ClientContext<DeviceTreeEngineAddedEvent> context, CancellationToken cancellationToken)
    {
        if (context.CorrelationId is null || !_newDeviceEngineRequests.TryGetValue(context.CorrelationId.Value, out var callbackTuples))
            return Task.CompletedTask;

        var deviceTreeConnectors = context.Message.DeviceTreeConnectors;
        var deviceUri = new UriBuilder(deviceTreeConnectors.DeviceAddress).Uri;
        _deviceTreeConnectors[deviceUri] = (deviceTreeConnectors.TriggerInput, deviceTreeConnectors.DeviceTreeOutput);
        var eventAddress = new UriBuilder(context.Message.Address).Uri;
        var callbackTuple = callbackTuples.FirstOrDefault(c => c.address == eventAddress);

        _ = Task.Run(async () =>
        {
            await _requestDevicesSemaphore.WaitAsync();

            try
            {
                await RequestExistingDeviceAsync(callbackTuple.deviceType, deviceUri, false, callbackTuple.callback);
            }
            catch (Exception ex)
            {
                LogRequestExistingDeviceFailedWarning(_logger, deviceUri, ex);
            }
            finally
            {
                _requestDevicesSemaphore.Release();
            }

        }, cancellationToken);

        callbackTuples.Remove(callbackTuple);

        if (callbackTuples.Count == 0)
        {
            _newDeviceEngineRequests.Remove(context.CorrelationId.Value);
        }

        return Task.CompletedTask;
    }

    public Task Consume(ClientContext<DeviceConnectorIdsChangedEvent> context, CancellationToken cancellationToken)
    {
        foreach (var changeItem in context.Message.ChangedItems.Where(i => i.Action is CrudAction.Created or CrudAction.Updated))
        {
            _deviceTreeConnectors[new UriBuilder(changeItem.Ids.DeviceAddress).Uri] = (changeItem.Ids.TriggerInput, changeItem.Ids.DeviceTreeOutput);
        }

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _deviceScannerEngineAddedSemaphore.Dispose();
        _requestDevicesSemaphore.Dispose();
        _deploymentResetEvent.Dispose();
        _autoDisposeList.Dispose();
    }

    public async Task<List<ValueMappingEntry>> GetOutputConnectorMappingAsync()
        => (await _mediator.Request<GetOutputConnectorMappingRequest, GetOutputConnectorMappingResponse>(new GetOutputConnectorMappingRequest()))
            .Mapping;

    private static DcpScanningResult ParseIoLinkScanResult(string value)
    {
        var scanningResult = DeserializeScanResult<DcpScanningResult>(value);
        EnsureScanSucceeded(scanningResult.Messages, scanningResult.Devices.Count);

        return scanningResult;
    }

    private static VseScanningResult ParseVseScanResult(string value)
    {
        var scanningResult = DeserializeScanResult<VseScanningResult>(value);
        EnsureScanSucceeded(scanningResult.Messages, scanningResult.Devices.Count);

        return scanningResult;
    }

    private static TResult DeserializeScanResult<TResult>(string value)
        => JsonSerializer.Deserialize<TResult>(value, SerializerOptions.DeviceTree)
            ?? throw new InvalidDataException($"Device scan failed: failed to deserialize result '{value[..(value.Length > 50 ? 50 : value.Length)]}' (chopped at 50 characters).");

    private static void EnsureScanSucceeded(IReadOnlyCollection<string> messages, int deviceCount)
    {
        if (messages.Count > 0 && deviceCount > 0)
            throw new InvalidDataException($"Device scan failed: {string.Join("; ", messages)}.");
    }

    public async Task<bool> IsClusterRunningAsync()
    {
        var clusterInfos = await _clusterService.QueryClusterInfosAsync();

        return clusterInfos.Count > 0
               && clusterInfos.OrderBy(c => c.Version).Last().IsActive;
    }

    private async Task LoadDeviceConnectorsAsync()
    {
        var deviceConnectorsResponse = await _mediator.Request<GetDeviceConnectorsRequest, GetDeviceConnectorsResponse>(new GetDeviceConnectorsRequest(null));
        if (deviceConnectorsResponse.RequestError is not null)
        {
            // TODO: get's logged in the backend but should be handled
        }

        foreach (var deviceConnectorIds in deviceConnectorsResponse.Ids)
        {
            _deviceTreeConnectors[new UriBuilder(deviceConnectorIds.DeviceAddress).Uri] = (deviceConnectorIds.TriggerInput, deviceConnectorIds.DeviceTreeOutput);
        }

        LogReturnsIdsDebug(_logger, nameof(LoadDeviceConnectorsAsync), deviceConnectorsResponse.Ids.Count);
    }

    public async Task RequestExistingDevicesAsync(IEnumerable<IDeviceTreeMasterNode> devices, bool triggerSubscriber, Func<IReadOnlyCollection<(IDeviceTreeMasterNode? device, Uri address, bool success)>, Task> callback)
    {
        var devicesArray = devices.ToArray();
        var errorDevices = 0;
        var receivedDevices = new List<(IDeviceTreeMasterNode? device, Uri address, bool success)>();

        if (devicesArray.Length == 0)
        {
            await callback(receivedDevices);
            return;
        }

        foreach (var device in devicesArray)
        {
            try
            {
                await RequestExistingDeviceAsync(device.GetType(), new UriBuilder(device.Url).Uri, triggerSubscriber, async (received, url, success) =>
                {
                    receivedDevices.Add((received, url, success));

                    if (devicesArray.Length <= receivedDevices.Count + errorDevices)
                    {
                        await callback(receivedDevices);
                    }
                });
            }
            catch (Exception ex)
            {
                LogRequestExistingDeviceFailedWarning(_logger, new UriBuilder(device.Url).Uri, ex);
                errorDevices++;

                if (devicesArray.Length <= receivedDevices.Count + errorDevices)
                {
                    await callback(receivedDevices);
                }
            }
        }
    }

    public Task RequestExistingDeviceAsync(Type type, Uri deviceAddress, bool triggerSubscriber, Func<IDeviceTreeMasterNode?, Uri, bool, Task> callback)
    {
        var methodInfo = GetType().GetMethod(nameof(RequestExistingDeviceAsync), 1, [typeof(Uri), typeof(bool), typeof(Func<IDeviceTreeMasterNode?, Uri, bool, Task>)]);
        methodInfo = methodInfo!.MakeGenericMethod(type);
        return (Task)methodInfo.Invoke(this, [deviceAddress, triggerSubscriber, callback])!;
    }

    public async Task RequestExistingDeviceAsync<T>(Uri deviceAddress, bool triggerSubscriber, Func<IDeviceTreeMasterNode?, Uri, bool, Task> callback) where T : IDeviceTreeMasterNode
    {
        var (deviceTreeTriggerId, deviceTreeOutputId) = _deviceTreeConnectors[deviceAddress];

        IAsyncDisposable? handle = null;
#pragma warning disable CA2000 // Dispose objects before losing scope
        var timeoutCancellation = new CancellationTokenSource();
#pragma warning restore CA2000 // Dispose objects before losing scope
        var skippedRetainedMessageAlready = false;

        LogSubscribingDeviceTree(_logger, deviceAddress, deviceTreeOutputId, deviceTreeTriggerId);

        try
        {
            handle = await _eventBroker.Subscribe(deviceTreeOutputId, async (d, v) =>
            {
                var device = v is not null
                    ? JsonSerializer.Deserialize<T>(v, SerializerOptions.DeviceTree)
                    : default;

                if (device is null || (!skippedRetainedMessageAlready && triggerSubscriber))
                {
                    skippedRetainedMessageAlready = true;

                    _ = Task.Run(async () =>
                    {
                        await Task.Delay(FrontendDeviceTimeout, timeoutCancellation.Token);

                        if (!timeoutCancellation.Token.IsCancellationRequested)
                        {
                            await handle!.DisposeAsync();
                            await timeoutCancellation.CancelAsync();
                            timeoutCancellation.Dispose();

                            LogTimeoutDidNotReceiveDeviceMessage(_logger, device?.Url.DnsSafeHost ?? "?");

                            if (device is not null)
                            {
                                var nodes = device.GetNodeAndDescendants();

                                foreach (var node in nodes)
                                {
                                    node.Status = ConnectionStatus.Offline;
                                }
                            }

                            await callback(null, deviceAddress, false);
                        }
                    });

                    if (triggerSubscriber)
                    {
                        LogSkipFirstMessageInformation(_logger, deviceAddress);
                    }
                    else
                    {
                        LogSkipFirstMessageWarning(_logger, deviceAddress);
                    }

                    return;
                }

                LogReceivedDeviceMessageInfo(_logger, deviceAddress);

                // Capture and clear the outer handle variable so the timeout path cannot race
                // on a double-dispose, then fire-and-forget the unsubscribe.  Disposing from
                // within the subscriber callback would call Unsubscribe while the broker's
                // dispatch loop is still on the call stack, risking a race between dictionary
                // removal and a concurrent re-subscribe for the same event.
                var handleToDispose = handle;
                handle = null;

                await timeoutCancellation.CancelAsync();
                timeoutCancellation.Dispose();

                if (!triggerSubscriber || skippedRetainedMessageAlready)
                {
                    DeviceTreeBuilder.RemoveEmptyStructureNodes(device);
                    await callback(device, deviceAddress, true);
                }

                if (handleToDispose is not null)
                {
                    _ = Task.Run(async () =>
                    {
                        try { await handleToDispose.DisposeAsync(); }
                        catch (Exception ex) { LogClusterSubscriptionFailed(_logger, ex); }
                    });
                }
            });
        }
        catch (Exception ex)
        {
            LogClusterSubscriptionFailed(_logger, ex);
            await callback(null, deviceAddress, false);
            return;
        }

        if (triggerSubscriber)
        {
            LogSendingDeviceTrigger(_logger, deviceAddress, deviceTreeTriggerId);

            try
            {
                await _eventBroker.SetValue(deviceTreeTriggerId, true.ToString());
            }
            catch
            {
                await callback(null, deviceAddress, false);
            }
        }
    }

    public async Task<DeviceTreeRoot> RequestDeviceTreeAsync()
    {
        await LoadDeviceConnectorsAsync();

        return (await _mediator.Request<GetDeviceTree, GetDeviceTreeResponse>(new GetDeviceTree())).DeviceTree;
    }

    public async Task RequestNewDeviceDeviceTreeAsync(Type deviceType, Uri address, Func<IDeviceTreeMasterNode?, Uri, bool, Task> callback, bool allowUseExistingEngine, LogLevel logLevel)
    {
        var command = new AddDeviceTreeEngine([new(address, deviceType.AssemblyQualifiedName!)], allowUseExistingEngine, logLevel);
        await _mediator.Send(command);
        _newDeviceEngineRequests.Add(command.CorrelationId, [(callback, deviceType, new UriBuilder(address).Uri)]);
    }

    public async Task RequestNewDevicesDeviceTreeAsync(IEnumerable<DeviceEngineInfo> deviceEngineInfos,
        Func<IDeviceTreeMasterNode?, Uri, bool, Task> callback, bool allowUseExistingEngine, LogLevel logLevel)
    {
        var engineInfos = deviceEngineInfos as DeviceEngineInfo[] ?? [.. deviceEngineInfos];
        var command = new AddDeviceTreeEngine(engineInfos, allowUseExistingEngine, logLevel);

        _newDeviceEngineRequests.Add(command.CorrelationId, []);
        _newDeviceEngineRequests[command.CorrelationId].AddRange(engineInfos.Select(e => (callback, Type.GetType(e.Type)!, new UriBuilder(e.Address).Uri)));

        await _mediator.Send(command);
    }

    public async Task SaveDeviceTreeAsync(IReadOnlyCollection<string> masterNodesToUpdate, IReadOnlyCollection<IDeviceTreeBase> deletedNodes,
        DeviceTreeRoot deviceTree, LogLevel logLevel)
    {
        _skipNextDeviceTreeChange = true;
        using var cts = new CancellationTokenSource();
        cts.CancelAfter(MaxTimeout);

        try
        {
            var tokenResponse = await _mediator.Request<GetDeviceTreeUpdateTokenRequest, GetDeviceTreeUpdateTokenResponse>(new GetDeviceTreeUpdateTokenRequest(), cts.Token);
            // TODO: Call "_deviceTreeUpdater.LoadDeviceTree()" and apply the user's changes,
            // instead of saving and possibly overriding changes directly
            await _mediator.Send(new UpdateDeviceTree(tokenResponse.Token, deviceTree, deletedNodes, masterNodesToUpdate, logLevel));
        }
        catch (ObjectDisposedException)
        {
            // ok
        }
        catch (OperationCanceledException)
        {
            LogFailedToApplyDeviceTreeTimeoutWhileWaitingForTicket();
        }
    }

    public Task<DcpScanningResult> ScanIoLinkDevicesAsync(LogLevel logLevel, CancellationToken cancellationToken)
        => ScanDevicesAsync(
            FunctionBlocks.IoLinkMasterScanner.Outputs.DevicesNodeId,
            FunctionBlocks.IoLinkMasterScanner.Inputs.TriggerNodeId,
            ParseIoLinkScanResult,
            messages => new DcpScanningResult { Devices = [], Messages = [.. messages] },
            logLevel,
            cancellationToken);

    public Task<VseScanningResult> ScanVseDevicesAsync(LogLevel logLevel, CancellationToken cancellationToken)
        => ScanDevicesAsync(
            FunctionBlocks.VseScanner.Outputs.DevicesNodeId,
            FunctionBlocks.VseScanner.Inputs.TriggerNodeId,
            ParseVseScanResult,
            messages => new VseScanningResult { Devices = [], Messages = [.. messages] },
            logLevel,
            cancellationToken);

    /// <summary>
    /// Runs one scan: subscribe to the scanner's result connector, deploying the scanner dataflow first if it
    /// is not there yet, then pulse its trigger and wait for the result.
    /// </summary>
    /// <typeparam name="TResult">The kind of result the scanner reports.</typeparam>
    /// <param name="devicesNodeId">The connector the scan result arrives on.</param>
    /// <param name="triggerNodeId">The connector a scan is started by.</param>
    /// <param name="parse">Turns a received message into a result.</param>
    /// <param name="failure">Builds a result that carries nothing but the given messages.</param>
    /// <param name="logLevel">The log level a newly deployed scanner engine runs at.</param>
    /// <param name="cancellationToken">Abandons the scan; the caller gets an empty result.</param>
    private async Task<TResult> ScanDevicesAsync<TResult>(
        Guid devicesNodeId,
        Guid triggerNodeId,
        Func<string, TResult> parse,
        Func<IEnumerable<string>, TResult> failure,
        LogLevel logLevel,
        CancellationToken cancellationToken)
    {
#pragma warning disable CA2000 // Dispose objects before losing scope
        SemaphoreSlim? resetEvent = new(0, 1);
#pragma warning restore CA2000 // Dispose objects before losing scope

        var progress = new ScanProgress<TResult> { Result = failure([]) };
        IAsyncDisposable? subscription = null;

        LogSubscribingScanOutput(_logger);

        var callback = new Func<DateTime, string?, Task>((t, value) =>
        {
            if (cancellationToken.IsCancellationRequested)
            {
                SignalSemaphore(resetEvent);
                return Task.CompletedTask;
            }

            return OnScanMessageReceived(value, resetEvent, progress, parse);
        });

        try
        {
            subscription = await _eventBroker.Subscribe(devicesNodeId, callback, cancellationToken);
        }
        catch
        {
            LogScannerSubscriptionFailedCreatingDataflow(_logger);

            await (subscription?.DisposeAsync() ?? ValueTask.CompletedTask);

            if (!await AddDeviceScannerDataflow(logLevel))
            {
                resetEvent.Dispose();
                return failure(["Failed to deploy the device scanner dataflow."]);
            }

            try
            {
                subscription = await _eventBroker.Subscribe(devicesNodeId, callback, cancellationToken);
            }
            catch (Exception ex)
            {
                LogScannerSubscriptionFailedUnexpectedly(_logger, ex);
                resetEvent.Dispose();
                await (subscription?.DisposeAsync() ?? ValueTask.CompletedTask);

                return failure([$"Failed to subscribe to device scanning output: {ex.GetType()} {ex.Message}."]);
            }
        }

        await TriggerScanAsync(triggerNodeId);

        var isTimeout = !await resetEvent.WaitAsync(DeviceScanTimeout, CancellationToken.None);

        if (cancellationToken.IsCancellationRequested)
        {
            resetEvent.Dispose();
            await subscription.DisposeAsync();
            return failure([]);
        }

        if (isTimeout)
        {
            LogNoScanDataReceived(_logger);

            // The retained message held back below is all there is once the scan itself did not answer.
            if (!string.IsNullOrEmpty(progress.FirstMessageResult))
            {
                try
                {
                    progress.Result = parse(progress.FirstMessageResult);
                }
                catch (Exception ex)
                {
                    LogScanResultSerializationFailed(_logger, ex);
                }
            }
        }

        resetEvent.Dispose();
        await subscription.DisposeAsync();
        return progress.Result;
    }

    /// <summary>
    /// Handles one message from a scanner's result connector. The first one is the value retained from the
    /// previous scan and is held back rather than reported, so only the message the current scan produces
    /// completes the wait.
    /// </summary>
    private Task OnScanMessageReceived<TResult>(string? value, SemaphoreSlim? resetEvent, ScanProgress<TResult> progress, Func<string, TResult> parse)
    {
        if (progress.FirstMessage)
        {
            LogScanReceivedFirstMessage(_logger);
            progress.FirstMessageResult = value;
            progress.FirstMessage = false;
            return Task.CompletedTask;
        }

        if (value == "null")
        {
            LogScanReceivedNullMessage(_logger);
            return Task.CompletedTask;
        }

        LogScanReceivedSecondMessage(_logger);

        if (!string.IsNullOrEmpty(value))
        {
            try
            {
                progress.Result = parse(value);
            }
            catch (Exception ex)
            {
                LogScanResultSerializationFailed(_logger, ex);
            }
        }

        SignalSemaphore(resetEvent);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Starts a scan. The trigger counts as changed on every write, so writing it back to false right away
    /// leaves it ready for the next scan.
    /// </summary>
    private async Task TriggerScanAsync(Guid triggerNodeId)
    {
        LogTriggeringScan(_logger);
        await _eventBroker.SetValue(triggerNodeId, true.ToString());
        await _eventBroker.SetValue(triggerNodeId, false.ToString());
    }

    /// <summary>
    /// What a running scan has seen so far.
    /// </summary>
    private sealed class ScanProgress<TResult>
    {
        /// <summary>Whether the next message will be the first one, which is the retained one.</summary>
        public bool FirstMessage { get; set; } = true;

        /// <summary>The retained message, kept in case the scan itself never answers.</summary>
        public string? FirstMessageResult { get; set; }

        /// <summary>The result reported so far.</summary>
        public TResult Result { get; set; } = default!;
    }

    public async Task<bool> IsDeployInProgressAsync()
    {
        var clusterInfo = (await _clusterService.QueryClusterInfosAsync()).OrderBy(i => i.Version).LastOrDefault();
        if (clusterInfo is null)
            return false;

        var stateResponse = await _mediator.Request<GetDeployClusterStateRequest, GetDeployClusterStateResponse>(new GetDeployClusterStateRequest(clusterInfo.Id));
        if (stateResponse.State is null)
            return false;

        // A non-null State alone does not mean a deployment is currently running - the server
        // keeps reporting the last known DeploymentState even once it's finished. The active and
        // target version only differ while a deployment to reach TargetVersion is actually in
        // progress; once it lands, ActiveVersion catches up and they match again.
        return stateResponse.State.ActiveVersion != stateResponse.State.TargetVersion;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="timeout">how long a running deployment is waited for</param>
    /// <returns>true if no deployment is running or it finishes before timeout.</returns>
    public async Task<bool> WaitForCurrentDeployment(TimeSpan timeout)
    {
        if (_deploymentInProgress || await IsDeployInProgressAsync())
        {
            await DrainSemaphoreAsync(_deploymentResetEvent);
            return await _deploymentResetEvent.WaitAsync(timeout);
        }

        return true;
    }

    public Task Consume(ClientContext<ClusterUpdateCompleted> context, CancellationToken cancellationToken)
    {
        _deploymentInProgress = false;
        SignalSemaphore(_deploymentResetEvent);
        return Task.CompletedTask;
    }

    private void InvokeDataInvalid(bool deviceTreeChanged)
    {
        if (deviceTreeChanged)
        {
            if (_skipNextDeviceTreeChange)
            {
                _skipNextDeviceTreeChange = false;
                return;
            }

            DataPossiblyInvalid?.Invoke(true);
            return;
        }

        if (_deploymentInProgress)
            return;

        if (_newDeviceEngineRequests.Count > 0 && !deviceTreeChanged)
            return;

        _deploymentInProgress = true;
        DataPossiblyInvalid?.Invoke(false);
    }

    public Task Consume(ClientContext<ClusterUpdateChanged> context, CancellationToken cancellationToken)
    {
        InvokeDataInvalid(false);
        return Task.CompletedTask;
    }

    public Task Consume(ClientContext<DeviceTreeChangedEvent> context, CancellationToken cancellationToken)
    {
        InvokeDataInvalid(true);
        return Task.CompletedTask;
    }

    public Task Consume(ClientContext<ClusterUpdateFailed> context, CancellationToken cancellationToken)
    {
        _deploymentInProgress = false;
        SignalSemaphore(_deploymentResetEvent);
        return Task.CompletedTask;
    }

    public Task Consume(ClientContext<ClusterUpdateRejected> context, CancellationToken cancellationToken)
    {
        _deploymentInProgress = false;
        SignalSemaphore(_deploymentResetEvent);
        return Task.CompletedTask;
    }

    public Task Consume(ClientContext<ClusterUpdateStarted> context, CancellationToken cancellationToken)
    {
        InvokeDataInvalid(false);
        return Task.CompletedTask;
    }

    public Task Consume(ClientContext<NodesOnlineEvent> context, CancellationToken cancellationToken)
        => NodesOnline?.Invoke([.. context.Message.OnlineNodes.Select(n => n.Id)]) ?? Task.CompletedTask;

    public Task Consume(ClientContext<NodesOfflineEvent> context, CancellationToken cancellationToken)
        => NodesOffline?.Invoke([.. context.Message.OfflineNodes.Select(n => n.Id)]) ?? Task.CompletedTask;

    private static void SignalSemaphore(SemaphoreSlim? semaphore)
    {
        if (semaphore is null)
            return;

        try
        {
            semaphore.Release();
        }
        catch (ObjectDisposedException) { }
        catch (SemaphoreFullException) { }
    }

    private static async Task DrainSemaphoreAsync(SemaphoreSlim semaphore)
    {
        while (semaphore.CurrentCount > 0)
        {
            if (!await semaphore.WaitAsync(0))
                break;
        }
    }
}
