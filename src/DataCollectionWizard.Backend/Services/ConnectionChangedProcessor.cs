using System.Timers;
using DataCollectionWizard.Internal.Extensions;
using DataCollectionWizard.Internal.Services.CloudDataflowGenerators;
using DataCollectionWizard.Public.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Sdk.Connections.Events;
using Sdk.Messaging;
using ViciOne.Driver.IoTCore.Contracts.DeviceTree;
using ViciOne.Driver.IoTCore.Contracts.DeviceTree.Extensions;

namespace DataCollectionWizard.Backend.Services;

public sealed partial class ConnectionChangedProcessor : IConnectionChangedProcessor
{
    private readonly ILogger<ConnectionChangedProcessor> _logger;
    private readonly Lock _queuedEventsLock = new();
    private readonly Queue<ConnectionChanged> _queuedEvents = [];
    private readonly ConnectionChangedProcessorState _state;
#pragma warning disable CA2213 // Disposable fields should be disposed
    // Scope is beeing disposed when timer elapses
    private readonly IServiceScope _serviceScope;
#pragma warning restore CA2213 // Disposable fields should be disposed
    private readonly IEnumerable<ICloudFilter> _cloudFilters;

    public ConnectionChangedProcessor(ConnectionChangedProcessorState state, IServiceProvider serviceProvider, IEnumerable<ICloudFilter> cloudFilters, ILogger<ConnectionChangedProcessor> logger)
    {
        _state = state;
        _serviceScope = serviceProvider.CreateScope();
        _cloudFilters = cloudFilters.ToArray();
        _logger = logger;

        _state.Timer.Interval = 500;
        _state.Timer.AutoReset = false;
        _state.Timer.Elapsed += OnTimerElapsed;
    }

    public void Enqueue(ConnectionChanged connectionChanged)
    {
        _state.Timer.Stop();

        lock (_queuedEventsLock)
        {
            _queuedEvents.Enqueue(connectionChanged);
        }

        _state.Timer.Start();
    }

    private void OnTimerElapsed(object? sender, ElapsedEventArgs e)
        => _ = Task.Run(async () =>
        {
            try
            {
                await OnTimerElapsedAsync();
            }
            catch (Exception ex)
            {
                LogErrorAfterConnectionChange(_logger, ex.GetType(), ex.Message, ex.StackTrace);
            }
            finally
            {
                _serviceScope.Dispose();
                _state.Timer.Elapsed -= OnTimerElapsed;
            }
        });

    private async Task OnTimerElapsedAsync()
    {
        ConnectionChanged[] events;

        lock (_queuedEventsLock)
        {
            events = _queuedEvents.ToArray();
            _queuedEvents.Clear();
        }

        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromSeconds(30));

        var deviceTreeUpdater = _serviceScope.ServiceProvider.GetRequiredService<IDeviceTreeUpdater>();

        var ticket = await deviceTreeUpdater.RequestUpdateAsync(cts.Token);
        try
        {
            var deviceTree = await deviceTreeUpdater.LoadDeviceTree();

            var relevantDevices = new List<string>();
            var saveTree = false;

            foreach (var changedEvent in events)
            {
                var cloudConnections = _cloudFilters
                    .SelectMany(f => f.GetCloudConnections([changedEvent.Connection]))
                    .DistinctBy(c => c.Id)
                    .ToArray();

                if (cloudConnections.Length == 0)
                    continue;

                if (changedEvent.Action == CrudAction.Created || changedEvent.Action == CrudAction.Updated)
                {
                    foreach (var dataNode in deviceTree.GetNodeAndDescendants().OfType<IDeviceTreeDataNode>())
                        dataNode.AddConfigurations(cloudConnections);

                    saveTree = true;
                    relevantDevices.AddRange(deviceTree.Children.OfType<IDeviceTreeMasterNode>()
                                                                .Select(m => m.Id));
                }
                else if (changedEvent.Action == CrudAction.Deleted)
                {
                    var publishTargetsIds = cloudConnections.Select(t => t.Id).ToHashSet();

                    foreach (var dataNode in deviceTree.GetNodeAndDescendants().OfType<IDeviceTreeDataNode>())
                        dataNode.RemoveConfigurations(publishTargetsIds);

                    saveTree = true;

                    relevantDevices.AddRange(deviceTree.Children.OfType<IDeviceTreeMasterNode>()
                                                                .Where(m => IsRelevantMasterNode(m, changedEvent.Connection.Id))
                                                                .Select(m => m.Id));
                }
            }

            if (relevantDevices.Count == 0)
            {
                deviceTreeUpdater.DiscardUpdateRequest(ticket);
                return;
            }

            await deviceTreeUpdater.UpdateDeviceTreeAsync(ticket, deviceTree, [], relevantDevices.Distinct(), saveTree: saveTree);
        }
        catch (Exception)
        {
            deviceTreeUpdater.DiscardUpdateRequest(ticket);
            throw;
        }
    }

    private static bool IsRelevantMasterNode(IDeviceTreeMasterNode masterNode, Guid connectionId)
    {
        var hasRelevantCompressableDataNodes = masterNode.GetNodeAndDescendants()
            .OfType<IDeviceTreeCompressableDataNode>()
            .Any(n => n.CompressorConfigurations.Any(c => c.Enabled && c.DataGroupIdentifier == connectionId));

        var hasRelevantEventTriggerDataNodes = masterNode.GetNodeAndDescendants()
            .OfType<IDeviceTreeEventTriggerDataNode>()
            .Any(n => n.EventTriggerConfigurations.Any(c => c.IsSensorConfigured || c.Triggers.Any(t => t.Enabled && t.DataGroupIdentifier == connectionId)));

        var hasRelevantSchedulableDataNodes = masterNode.GetNodeAndDescendants()
            .OfType<IDeviceTreeSchedulableDataNode>()
            .Any(n => n.SchedulerConfigurations.Any(c => c.Enabled && c.DataGroupIdentifier == connectionId));

        return hasRelevantCompressableDataNodes
               || hasRelevantEventTriggerDataNodes
               || hasRelevantSchedulableDataNodes;
    }

    [LoggerMessage(LogLevel.Warning, "An error occurred after connection change queue elapsed: {exType}, {message} {stackTrace}")]
    public static partial void LogErrorAfterConnectionChange(ILogger logger, Type exType, string message, string? stackTrace);
}
