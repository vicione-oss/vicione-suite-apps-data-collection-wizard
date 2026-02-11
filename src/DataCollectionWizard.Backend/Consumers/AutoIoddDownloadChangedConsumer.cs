using ClusterManagement.Public.Iodds.Events;
using DataCollectionWizard.Public.Services;
using MassTransit;
using Microsoft.Extensions.Logging;
using ViciOne.Driver.IoTCore.Contracts.DeviceTree;

namespace DataCollectionWizard.Backend.Consumers;

public sealed partial class AutoIoddDownloadChangedConsumer(IDeviceTreeUpdater deviceTreeUpdater,
                                                            ILogger<AutoIoddDownloadChangedConsumer> logger) :
    IConsumer<AutoIoddDownloadChanged>
{
    public async Task Consume(ConsumeContext<AutoIoddDownloadChanged> context)
    {
        LogConsume(logger,
             nameof(AutoIoddDownloadChanged),
             context.CorrelationId);

        try
        {
            var ticket = await deviceTreeUpdater.RequestUpdateAsync(context.CancellationToken, context.CorrelationId);
            var deviceTree = await deviceTreeUpdater.LoadDeviceTree(context.CancellationToken);
            var iolinkDevices = deviceTree.Children.OfType<DeviceTreeIoLinkMaster>().Select(d => d.Id).ToList();

            if (iolinkDevices.Count == 0)
            {
                LogNoDevices(logger);
                deviceTreeUpdater.DiscardUpdateRequest(ticket);
                return;
            }

            await deviceTreeUpdater.UpdateDeviceTreeAsync(ticket, deviceTree, [], iolinkDevices, null, false, context.CancellationToken);
        }
        catch (Exception ex)
        {
            LogApplicationFailedError(logger, ex.Message, ex.StackTrace ?? string.Empty);
        }
    }

    [LoggerMessage(LogLevel.Error, "Failed to apply DeviceTree: {message} {stackTrace}")]
    public static partial void LogApplicationFailedError(ILogger logger, string message, string stackTrace);

    [LoggerMessage(LogLevel.Debug, "Consume {command} CorrelationId: {correlationId}")]
    public static partial void LogConsume(ILogger logger, string command, Guid? correlationId);

    [LoggerMessage(LogLevel.Debug, "No io link devices found, skipping dataflow generation.")]
    public static partial void LogNoDevices(ILogger logger);
}
