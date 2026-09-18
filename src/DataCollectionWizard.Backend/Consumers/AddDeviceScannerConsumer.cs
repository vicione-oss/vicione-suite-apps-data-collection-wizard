using DataCollectionWizard.Backend.Services;
using DataCollectionWizard.Internal.Commands;
using DataCollectionWizard.Internal.Events;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace DataCollectionWizard.Backend.Consumers;

public sealed partial class AddDeviceScannerConsumer(
    IDataCollectionWizardService dataCollectionWizardService,
    IClusterService clusterService,
    ILogger<AddDeviceScannerConsumer> logger)
    : IConsumer<AddDeviceScanner>
{
    public async Task Consume(ConsumeContext<AddDeviceScanner> context)
    {
        LogConsume(logger,
            nameof(AddDeviceScanner),
            context.CorrelationId);

        Guid? ticket = null;
        try
        {
            ticket = await clusterService.RequestUpdateAsync(context.CancellationToken, context.CorrelationId);
            var cluster = await dataCollectionWizardService.AddDeviceScannerAsync(context.Message.LogLevel);
            if (cluster is null)
            {
                clusterService.DiscardUpdateRequest(ticket.Value);
                await context.Publish(new DeviceScannerEngineAddedEvent(false));
                return;
            }

            await clusterService.DeployClusterAsync(ticket.Value, cluster, context.CancellationToken);
            await context.Publish(new DeviceScannerEngineAddedEvent(true));
        }
        catch (Exception ex)
        {
            if (ticket is not null)
                clusterService.DiscardUpdateRequest(ticket.Value);
            LogError(logger, ex);
        }
    }
}
