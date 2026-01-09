using System.Collections.Concurrent;
using System.Timers;
using DataCollectionWizard.Internal.Extensions;
using DataCollectionWizard.Internal.Services.CloudDataflowGenerators;
using DataCollectionWizard.Public.Services;
using Microsoft.Extensions.Logging;
using Sdk.Connections.Events;
using Sdk.Messaging;
using ViciOne.Driver.IoTCore.Contracts.DeviceTree;
using ViciOne.Driver.IoTCore.Contracts.DeviceTree.Extensions;

namespace DataCollectionWizard.Backend.Services;

public interface IConnectionChangedProcessor
{
    void Enqueue(ConnectionChanged connectionChanged);
}

public sealed partial class ConnectionChangedProcessor : IConnectionChangedProcessor, IDisposable
{
    private readonly ILogger<ConnectionChangedProcessor> _logger;
    private readonly ConcurrentQueue<ConnectionChanged> _queuedEvents = [];
    private readonly ConnectionChangedProcessorState _state;
    private readonly IDeviceTreeUpdater _deviceTreeUpdater;
    private readonly IEnumerable<ICloudFilter> _cloudFilters;

    public ConnectionChangedProcessor(ConnectionChangedProcessorState state, IDeviceTreeUpdater deviceTreeUpdater, IEnumerable<ICloudFilter> cloudFilters, ILogger<ConnectionChangedProcessor> logger)
    {
        _state = state;
        _deviceTreeUpdater = deviceTreeUpdater;
        _cloudFilters = cloudFilters;
        _logger = logger;

        _state.Timer.Interval = 500;
        _state.Timer.AutoReset = false;
        _state.Timer.Elapsed += OnTimerElapsed;
    }

    public void Enqueue(ConnectionChanged connectionChanged)
    {
        _state.Timer.Stop();
        _queuedEvents.Enqueue(connectionChanged);
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
        });

    private async Task OnTimerElapsedAsync()
    {
        var events = _queuedEvents.ToArray();
        _queuedEvents.Clear();

        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromSeconds(30));

        var ticket = await _deviceTreeUpdater.RequestUpdateAsync(cts.Token);
        var deviceTree = await _deviceTreeUpdater.LoadDeviceTree();

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

            if (changedEvent.Action == CrudAction.Created)
            {
                foreach (var dataNode in deviceTree.GetNodeAndDescendants().OfType<IDeviceTreeDataNode>())
                    dataNode.AddConfigurations(cloudConnections);
                saveTree = true;
            }

            if (changedEvent.Action == CrudAction.Deleted)
            {
                var publishTargetsIds = cloudConnections.Select(t => t.Id).ToArray();
                foreach (var dataNode in deviceTree.GetNodeAndDescendants().OfType<IDeviceTreeDataNode>())
                    dataNode.RemoveConfigurations(publishTargetsIds);
                saveTree = true;
            }

            relevantDevices.AddRange(deviceTree.Children
                .OfType<IDeviceTreeMasterNode>()
                .Where(m => IsRelevantMasterNode(m, changedEvent.Connection.Id))
                .Select(m => m.Id));
        }

        if (relevantDevices.Count == 0)
            return;

        await _deviceTreeUpdater.UpdateDeviceTreeAsync(ticket, deviceTree, [], relevantDevices, saveTree: saveTree);
    }

    public void Dispose() => _state.Timer.Elapsed -= OnTimerElapsed;

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
