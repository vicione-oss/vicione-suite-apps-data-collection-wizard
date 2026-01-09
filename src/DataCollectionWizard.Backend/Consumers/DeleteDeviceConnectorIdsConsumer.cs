using DataCollectionWizard.Backend.DbContext;
using DataCollectionWizard.Internal.Commands;
using DataCollectionWizard.Internal.Events;
using DataCollectionWizard.Public;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sdk.Messaging;

namespace DataCollectionWizard.Backend.Consumers;

public sealed partial class DeleteDeviceConnectorIdsConsumer(IDataCollectionWizardDbContext dbContext, ILogger<DeleteDeviceConnectorIdsConsumer> logger) : IConsumer<DeleteDeviceConnectorIds>
{
    public async Task Consume(ConsumeContext<DeleteDeviceConnectorIds> context)
    {
        LogConsume(logger,
            nameof(DeleteDeviceConnectorIds),
            context.CorrelationId,
            context.Message.DeviceConnectorIds.Count);

        var changes = new List<DeviceConnectorIdsChangeItem>();

        foreach (var item in context.Message.DeviceConnectorIds)
        {
            var existing = await dbContext.DeviceConnectorIds
                .FirstOrDefaultAsync(d => d.Equals(item));

            if (existing is not null)
            {
                dbContext.DeviceConnectorIds.Remove(existing);
                changes.Add(new(CrudAction.Deleted, item));
            }
        }

        try
        {
            var dbChanges = await dbContext.Instance.SaveChangesAsync(context.CancellationToken);

            await context.Publish(new DeviceConnectorIdsChangedEvent(dbChanges > 0 ? changes : [])).ConfigureAwait(false);
        }
        catch (DbUpdateException e)
        {
            await context.Publish(new DeviceConnectorIdsChangeErrorEvent(new ErrorInfo(ErrorCodes.DbUpdateFailed, e.Message)));
        }
    }

    [LoggerMessage(LogLevel.Debug, "Consume {command} CorrelationId:{correlationId} DeviceConnectorIds.Count:{count}")]
    public static partial void LogConsume(ILogger logger, string command, Guid? correlationId, int count);
}
