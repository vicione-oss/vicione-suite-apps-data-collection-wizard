using DataCollectionWizard.Backend.Services;
using DataCollectionWizard.Internal.Commands;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace DataCollectionWizard.Backend.Consumers;

public sealed partial class AddDeviceTreeEngineConsumer(IDataCollectionWizardService dataCollectionWizardService,
    IClusterService clusterService,
    ILogger<AddDeviceTreeEngineConsumer> logger) : IConsumer<AddDeviceTreeEngine>
{
    public async Task Consume(ConsumeContext<AddDeviceTreeEngine> context)
    {
        LogConsume(logger,
            nameof(AddDeviceTreeEngine),
            context.CorrelationId,
            string.Join(", ", context.Message.Infos.Select(i => $"[{i.Type}|{i.Address}]")));

        Guid? ticket = null;
        try
        {
            ticket = await clusterService.RequestUpdateAsync(context.CancellationToken, context.CorrelationId);
            var updatedCluster = await dataCollectionWizardService.AddDeviceTreeEnginesAsync(context.Message.Infos, context.Message.CorrelationId, context.Message.AllowUseExistingEngine, context.Message.LogLevel);

            if (updatedCluster is null)
            {
                clusterService.DiscardUpdateRequest(ticket.Value);
                return;
            }
            await clusterService.DeployClusterAsync(ticket.Value, updatedCluster, context.CancellationToken);
        }
        catch (Exception ex)
        {
            if (ticket is not null)
                clusterService.DiscardUpdateRequest(ticket.Value);
            LogError(logger, ex);
        }
    }
}
