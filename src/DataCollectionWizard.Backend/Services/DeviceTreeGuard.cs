using System.Text.Json;
using ClusterManagement.Public.DataflowEvents;
using ClusterManagement.Public.Services;
using DataCollectionWizard.Internal;
using DataCollectionWizard.Internal.Events;
using DataCollectionWizard.Internal.Requests;
using DataCollectionWizard.Internal.Services;
using DataCollectionWizard.Public.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Sdk.Backend.Messaging;
using ViciOne.Driver.IoTCore.Contracts.DeviceTree;
using ViciOne.Driver.IoTCore.Contracts.DeviceTree.Extensions;

namespace DataCollectionWizard.Backend.Services;

public sealed partial class DeviceTreeGuard : IDeviceTreeGuard, IAsyncDisposable
{
    private readonly Lock _deviceTreeLock = new();
    private DeviceTreeRoot? _deviceTree;
    private readonly Lock _deviceTreeConnectorsLock = new();
    private readonly Dictionary<Uri, (Guid deviceTreeTrigger, Guid deviceTreeOutput)> _deviceTreeConnectors = [];
    private readonly AsyncServiceScope _eventBrokerScope;
    private readonly List<string> _lastOfflineNodes = [];
    private readonly Lock _lastOfflineNodesLock = new();
    private readonly ILogger<DeviceTreeGuard> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly Lock _subscriptionHandlesLock = new();
    private readonly Dictionary<string, IAsyncDisposable> _subscriptionHandles = [];
    private IDeviceTreeBase[] _untrackedNodes = [];
    private readonly Lock _untrackedNodesLock = new();

    public DeviceTreeGuard(IServiceProvider serviceProvider, ILogger<DeviceTreeGuard> logger)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _eventBrokerScope = serviceProvider.CreateAsyncScope();

        _ = InitDeviceTreeGuard();
    }

    private async Task DeviceEventHandler(Type deviceType, string? deviceTreeJson)
    {
        var device = deviceTreeJson is not null
                    ? JsonSerializer.Deserialize(deviceTreeJson, deviceType, SerializerOptions.DeviceTree)
                    : default;

        if (device is not IDeviceTreeMasterNode masterNode)
        {
            LogWrongDeviceTypeInformation(_logger);
            return;
        }

        IDeviceTreeBase? currentDevice;

        lock (_deviceTreeLock)
        {
            currentDevice = _deviceTree?.Children.FirstOrDefault(c => c.Id == masterNode.Id);
        }

        if (currentDevice is null)
        {
            LogWrongDeviceNotInDeviceTreeInformation(_logger);
            return;
        }

        using var scope = _serviceProvider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<ISuiteMediator>();
        var masterNodeDescendants = masterNode.GetNodeAndDescendants().ToArray();
        var nodeAndDescendants = currentDevice.GetNodeAndDescendants().ToArray();
        var untrackedNodes = masterNodeDescendants.ExceptBy(nodeAndDescendants.Select(n => n.Id), n => n.Id).ToArray();
        IDeviceTreeBase[] lastUntrackedNodes = [];

        lock (_untrackedNodesLock)
        {
            lastUntrackedNodes = _untrackedNodes.ToArray();
            _untrackedNodes = untrackedNodes;
        }

        if (masterNode.IsOffline)
        {
            var allOfflineNodes = currentDevice.GetNodeAndDescendants().Union(lastUntrackedNodes).ToArray();
            LogTriggeringNodesOfflineMasterOffline(_logger, allOfflineNodes.Length);
            await mediator.Publish(new NodesOfflineEvent(allOfflineNodes));

            lock (_lastOfflineNodesLock)
            {
                _lastOfflineNodes.AddRange(allOfflineNodes.Select(n => n.Id));
            }

            return;
        }

        DeviceTreeBuilder.UpdateOnlineStatus(DeviceTreeBuilder.CorrelateParsedDevices(currentDevice, [masterNode]));

        IDeviceTreeBase[] newOnlineNodes = [];
        IDeviceTreeBase[] newOfflineNodes = [];

        lock (_lastOfflineNodesLock)
        {
            newOnlineNodes = nodeAndDescendants.Union(lastUntrackedNodes)
                                               .Where(n => !n.IsOffline)
                                               .Where(n => _lastOfflineNodes.Contains(n.Id))
                                               .Union(untrackedNodes.ExceptBy(lastUntrackedNodes.Select(n => n.Id), n => n.Id)
                                                                    .ExceptBy(nodeAndDescendants.Where(n => !n.IsOffline).Select(n => n.Id), n => n.Id))
                                               .ToArray();

            newOfflineNodes = nodeAndDescendants.Union(lastUntrackedNodes)
                                                .Where(n => n.IsOffline)
                                                .Where(n => !_lastOfflineNodes.Contains(n.Id))
                                                .Union(lastUntrackedNodes.ExceptBy(untrackedNodes.Select(n => n.Id), n => n.Id)
                                                                            .ExceptBy(nodeAndDescendants.Select(n => n.Id), n => n.Id))
                                                .ToArray();

            _lastOfflineNodes.AddRange(newOfflineNodes.Select(n => n.Id));

            foreach (var newOnlineNode in newOnlineNodes)
            {
                _lastOfflineNodes.Remove(newOnlineNode.Id);
            }
        }

        if (newOfflineNodes.Length > 0)
        {
            LogTriggeringNodesOffline(_logger, newOfflineNodes.Length);
            await mediator.Publish(new NodesOfflineEvent(newOfflineNodes));
        }

        if (newOnlineNodes.Length > 0)
        {
            LogTriggeringNodesOnline(_logger, newOnlineNodes.Length);
            await mediator.Publish(new NodesOnlineEvent(newOnlineNodes));
        }
    }

    public async ValueTask DisposeAsync()
    {
        IAsyncDisposable[] handles;

        lock (_subscriptionHandlesLock)
        {
            handles = [.. _subscriptionHandles.Values];
            _subscriptionHandles.Clear();
        }

        foreach (var handle in handles)
        {
            try
            {
                await handle.DisposeAsync();
            }
            catch (Exception ex)
            {
                LogDisposeSubscriptionHandleFailed(_logger, ex);
            }
        }

        await _eventBrokerScope.DisposeAsync();
    }

    private async Task InitDeviceTreeGuard()
    {
        await LoadDeviceConnectorsAsync();
        await RequestDeviceTree();

        using var scope = _serviceProvider.CreateScope();
        var downloadState = scope.ServiceProvider.GetRequiredService<IResourceDownloadStateService>();

        await downloadState.WaitForCompletion();
        await UpdateSubscriptionsAsync();
    }

    private async Task RequestDeviceTree()
    {
        using var scope = _serviceProvider.CreateScope();
        var dataCollectionWizardService = scope.ServiceProvider.GetRequiredService<IDataCollectionWizardService>();

        var tree = await dataCollectionWizardService.RequestDeviceTreeAsync(CancellationToken.None);

        lock (_deviceTreeLock)
        {
            _deviceTree = tree;
        }
    }

    private async Task UpdateSubscriptionsAsync()
    {
        LogUpdatingDeviceTreeGuardSubscriptions(_logger);

        List<IDeviceTreeMasterNode>? masterDevices;

        lock (_deviceTreeLock)
        {
            masterDevices = _deviceTree!.GetNodeAndDescendants().OfType<IDeviceTreeMasterNode>().ToList();
        }

        var eventBroker = _eventBrokerScope.ServiceProvider.GetRequiredService<IEventBroker>();

        foreach (var masterDevice in masterDevices)
        {
            (Guid deviceTreeTrigger, Guid deviceTreeOutput) deviceTreeConnectors;
            bool hasConnector;

            lock (_deviceTreeConnectorsLock)
            {
                hasConnector = _deviceTreeConnectors.TryGetValue(masterDevice.Url, out deviceTreeConnectors);
            }

            if (hasConnector)
            {
                IAsyncDisposable? existingSubscriptionHandle;

                lock (_subscriptionHandlesLock)
                {
                    _subscriptionHandles.TryGetValue(masterDevice.Id, out existingSubscriptionHandle);
                }

                if (existingSubscriptionHandle is not null)
                {
                    await existingSubscriptionHandle.DisposeAsync();
                }

                var newHandle = await eventBroker.Subscribe(deviceTreeConnectors.deviceTreeOutput, (t, v) => DeviceEventHandler(masterDevice.GetType(), v));

                lock (_subscriptionHandlesLock)
                {
                    _subscriptionHandles[masterDevice.Id] = newHandle;
                }
            }
            else
            {
                LogNoDeviceTreeConnectorWarning(_logger, masterDevice.Url);
            }
        }
    }

    private async Task LoadDeviceConnectorsAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<ISuiteMediator>();

        var deviceConnectorsResponse = await mediator.Request<GetDeviceConnectorsRequest, GetDeviceConnectorsResponse>(new GetDeviceConnectorsRequest(null));

        lock (_deviceTreeConnectorsLock)
        {
            _deviceTreeConnectors.Clear();

            foreach (var deviceConnectorIds in deviceConnectorsResponse.Ids)
            {
                _deviceTreeConnectors[new UriBuilder(deviceConnectorIds.DeviceAddress).Uri] = (deviceConnectorIds.TriggerInput, deviceConnectorIds.DeviceTreeOutput);
            }
        }
    }

    public async Task OnDeviceConnectorIdsChanged(List<DeviceConnectorIdsChangeItem> changedItems)
        => await LoadDeviceConnectorsAsync();

    public async Task OnDeviceTreeApplication()
    {
        DeviceTreeRoot? currentDeviceTree;

        lock (_deviceTreeLock)
        {
            currentDeviceTree = _deviceTree;
        }

        await RequestDeviceTree();
        await UpdateSubscriptionsAsync();

        DeviceTreeRoot? newDeviceTree;

        lock (_deviceTreeLock)
        {
            newDeviceTree = _deviceTree;
        }

        DeviceTreeBuilder.UpdateOnlineStatus(DeviceTreeBuilder.CorrelateParsedDevices(currentDeviceTree!, newDeviceTree!.Children));

        var offlineNodes = currentDeviceTree!.GetNodeAndDescendants()
                                             .Where(n => n.IsOffline)
                                             .ToArray();

        if (offlineNodes.Length > 0)
        {
            using var scope = _serviceProvider.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<ISuiteMediator>();
            LogTriggeringNodesOfflineDeviceTreeApplied(_logger, offlineNodes.Length);
            await mediator.Publish(new NodesOfflineEvent(offlineNodes));
        }
    }
}
