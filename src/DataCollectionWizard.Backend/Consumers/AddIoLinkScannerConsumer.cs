using DataCollectionWizard.Backend.Services;
using DataCollectionWizard.Internal.Commands;
using DataCollectionWizard.Internal.Events;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace DataCollectionWizard.Backend.Consumers;

public sealed partial class AddIoLinkScannerConsumer(
    IDataCollectionWizardService dataCollectionWizardService,
    IClusterService clusterService,
    ILogger<AddIoLinkScannerConsumer> logger)
    : IConsumer<AddIoLinkScanner>
{
    public async Task Consume(ConsumeContext<AddIoLinkScanner> context)
    {
        LogConsume(logger,
            nameof(AddIoLinkScanner),
            context.CorrelationId);

        Guid? ticket = null;
        try
        {
            ticket = await clusterService.RequestUpdateAsync(context.CancellationToken, context.CorrelationId);
            var cluster = await dataCollectionWizardService.AddIoLinkScannerAsync(context.Message.LogLevel);
            if (cluster is null)
            {
                clusterService.DiscardUpdateRequest(ticket.Value);
                await context.Publish(new IoLinkScannerEngineAddedEvent(false));
                return;
            }

            await clusterService.DeployClusterAsync(ticket.Value, cluster, context.CancellationToken);
            await context.Publish(new IoLinkScannerEngineAddedEvent(true));
        }
        catch (Exception ex)
        {
            if (ticket is not null)
                clusterService.DiscardUpdateRequest(ticket.Value);
            LogError(logger, ex.Message, ex.StackTrace);
        }
    }

    [LoggerMessage(LogLevel.Debug, "Consume {command} CorrelationId:{correlationId}")]
    public static partial void LogConsume(ILogger logger, string command, Guid? correlationId);

    [LoggerMessage(LogLevel.Error, "Error trying to add DeviceTreeRequestEngine: {message} {stackTrace}")]
    public static partial void LogError(ILogger logger, string message, string? stackTrace);
}
