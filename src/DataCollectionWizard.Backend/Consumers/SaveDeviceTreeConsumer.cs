using DataCollectionWizard.Backend.DbContext;
using DataCollectionWizard.Backend.Extensions;
using DataCollectionWizard.Backend.Services;
using DataCollectionWizard.Internal.Commands;
using DataCollectionWizard.Internal.Events;
using DataCollectionWizard.Public;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sdk.Messaging;

namespace DataCollectionWizard.Backend.Consumers;

public sealed partial class SaveDeviceTreeConsumer(
    IDataCollectionWizardDbContext dbContext,
    DeviceTreeUpdaterState deviceTreeUpdaterState,
    ILogger<SaveDeviceTreeConsumer> logger)
        : IConsumer<SaveDeviceTree>
{
    public async Task Consume(ConsumeContext<SaveDeviceTree> context)
    {
        LogConsume(logger,
            nameof(SaveDeviceTree),
            context.CorrelationId);

        dbContext.UpsertDeviceTree(context.Message.DeviceTree);

        try
        {
            if (await dbContext.Instance.SaveChangesAsync(context.CancellationToken) > 0)
            {
                await context.Publish(new DeviceTreeChangedEvent(CrudAction.Updated));
            }

            deviceTreeUpdaterState.SetResult(context.CorrelationId);
        }
        catch (DbUpdateException e)
        {
            var error = new ErrorInfo(ErrorCodes.DbUpdateFailed, e.Message);
            await context.Publish(new DeviceTreeChangeErrorEvent(error));
            deviceTreeUpdaterState.SetResult(context.CorrelationId, error);
        }
    }

    [LoggerMessage(LogLevel.Debug, "Consume {command} CorrelationId: {correlationId}")]
    public static partial void LogConsume(ILogger logger, string command, Guid? correlationId);
}
