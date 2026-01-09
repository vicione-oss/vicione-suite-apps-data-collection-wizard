using System.Text.Json;
using ClusterManagement.Public.DataflowEvents;
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
    private DeviceTreeRoot? _deviceTree;
    private readonly Dictionary<Uri, (Guid deviceTreeTrigger, Guid deviceTreeOutput)> _deviceTreeConnectors = [];
    private readonly AsyncServiceScope _eventBrokerScope;
    private readonly List<string> _lastOfflineNodes = [];
    private readonly ILogger<DeviceTreeGuard> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly Dictionary<string, IAsyncDisposable> _subscriptionHandles = [];
    private IDeviceTreeBase[] _untrackedNodes = [];

    public DeviceTreeGuard(IServiceProvider serviceProvider, ILogger<DeviceTreeGuard> logger)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _eventBrokerScope = serviceProvider.CreateAsyncScope();

        _ = InitDeviceTreeGuard();
    }

    private async Task AwaitLoadFunctionBlocks()
    {
        if (_deviceTreeConnectors.Count == 0)
            return;

        LogIgnoreErrors(_logger);
        var eventBroker = _eventBrokerScope.ServiceProvider.GetRequiredService<IEventBroker>();

        for (var count = 0; count < 20; count++)
        {
            IAsyncDisposable? handle = null;

            try
            {
                handle = await eventBroker.Subscribe(_deviceTreeConnectors.Values.ElementAt(0).deviceTreeOutput, (t, v) => Task.CompletedTask);
                await handle.DisposeAsync();
                return;
            }
            catch
            {
                await Task.Delay(10000);
            }
            finally
            {
                if (handle is not null)
                {
                    await handle.DisposeAsync();
                }
            }
        }

        LogLoadClusterError(_logger);
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

        var currentDevice = _deviceTree?.Children.FirstOrDefault(c => c.Id == masterNode.Id);

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
        var lastUntrackedNodes = _untrackedNodes;
        _untrackedNodes = untrackedNodes;

        if (masterNode.IsOffline)
        {
            var allOfflineNodes = currentDevice.GetNodeAndDescendants().Union(lastUntrackedNodes).ToArray();
            LogTriggeringNodesOfflineMasterOffline(_logger, allOfflineNodes.Length);
            await mediator.Publish(new NodesOfflineEvent(allOfflineNodes));
            _lastOfflineNodes.AddRange(allOfflineNodes.Select(n => n.Id));
            return;
        }

        DeviceTreeBuilder.UpdateOnlineStatus(DeviceTreeBuilder.CorrelateParsedDevices(currentDevice, [masterNode]));

        var newOnlineNodes = nodeAndDescendants.Union(lastUntrackedNodes)
                                               .Where(n => !n.IsOffline)
                                               .Where(n => _lastOfflineNodes.Contains(n.Id))
                                               .Union(untrackedNodes.ExceptBy(lastUntrackedNodes.Select(n => n.Id), n => n.Id)
                                                                    .ExceptBy(nodeAndDescendants.Where(n => !n.IsOffline).Select(n => n.Id), n => n.Id))
                                               .ToArray();

        var newOfflineNodes = nodeAndDescendants.Union(lastUntrackedNodes)
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
        => await _eventBrokerScope.DisposeAsync();

    private async Task InitDeviceTreeGuard()
    {
        await LoadDeviceConnectorsAsync();
        await RequestDeviceTree();
        await AwaitLoadFunctionBlocks();
        await UpdateSubscriptionsAsync();
    }

    private async Task RequestDeviceTree()
    {
        using var scope = _serviceProvider.CreateScope();
        var dataCollectionWizardService = scope.ServiceProvider.GetRequiredService<IDataCollectionWizardService>();
        _deviceTree = await dataCollectionWizardService.RequestDeviceTreeAsync(CancellationToken.None);
    }

    private async Task UpdateSubscriptionsAsync()
    {
        LogUpdatingDeviceTreeGuardSubscriptions(_logger);
        var masterDevices = _deviceTree!.GetNodeAndDescendants().OfType<IDeviceTreeMasterNode>().ToList();
        var eventBroker = _eventBrokerScope.ServiceProvider.GetRequiredService<IEventBroker>();

        foreach (var masterDevice in masterDevices)
        {
            if (_deviceTreeConnectors.TryGetValue(masterDevice.Url, out var deviceTreeConnectors))
            {
                if (_subscriptionHandles.TryGetValue(masterDevice.Id, out var existingSubscriptionHandle))
                {
                    await existingSubscriptionHandle.DisposeAsync();
                }

                _subscriptionHandles[masterDevice.Id] = await eventBroker.Subscribe(deviceTreeConnectors.deviceTreeOutput, (t, v) => DeviceEventHandler(masterDevice.GetType(), v));
            }
            else
            {
                LogNoDeviceTreeConnectorWarning(_logger, masterDevice.Url);
            }
        }
    }

    private async Task LoadDeviceConnectorsAsync()
    {
        _deviceTreeConnectors.Clear();

        using var scope = _serviceProvider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<ISuiteMediator>();

        var deviceConnectorsResponse = await mediator.Request<GetDeviceConnectorsRequest, GetDeviceConnectorsResponse>(new GetDeviceConnectorsRequest(null));

        foreach (var deviceConnectorIds in deviceConnectorsResponse.Ids)
        {
            _deviceTreeConnectors[new UriBuilder(deviceConnectorIds.DeviceAddress).Uri] = (deviceConnectorIds.TriggerInput, deviceConnectorIds.DeviceTreeOutput);
        }
    }

    public async Task OnDeviceConnectorIdsChanged(List<DeviceConnectorIdsChangeItem> changedItems)
        => await LoadDeviceConnectorsAsync();

    public async Task OnDeviceTreeApplication()
    {
        var currentDeviceTree = _deviceTree;
        await RequestDeviceTree();
        await UpdateSubscriptionsAsync();
        DeviceTreeBuilder.UpdateOnlineStatus(DeviceTreeBuilder.CorrelateParsedDevices(currentDeviceTree!, _deviceTree!.Children));

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
