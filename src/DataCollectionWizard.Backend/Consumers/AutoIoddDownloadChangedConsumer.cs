using ClusterManagement.Public.Iodds.Events;
using DataCollectionWizard.Public.Services;
using MassTransit;
using Microsoft.Extensions.Logging;
using ViciOne.DeviceTree.Contracts;

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

        Guid? ticket = null;
        try
        {
            ticket = await deviceTreeUpdater.RequestUpdateAsync(context.CancellationToken, context.CorrelationId);
            var deviceTree = await deviceTreeUpdater.LoadDeviceTree(context.CancellationToken);
            var iolinkDevices = deviceTree.Children.OfType<DeviceTreeIoLinkMaster>().Select(d => d.Id).ToList();

            if (iolinkDevices.Count == 0)
            {
                LogNoDevices(logger);
                deviceTreeUpdater.DiscardUpdateRequest(ticket.Value);
                return;
            }

            await deviceTreeUpdater.UpdateDeviceTreeAsync(ticket.Value, deviceTree, [], iolinkDevices, null, false, context.CancellationToken);
        }
        catch (Exception ex)
        {
            if (ticket is not null)
                deviceTreeUpdater.DiscardUpdateRequest(ticket.Value);
            LogApplicationFailedError(logger, ex);
        }
    }
}
