using DataCollectionWizard.Backend.DbContext;
using DataCollectionWizard.Internal.Commands;
using DataCollectionWizard.Internal.Events;
using DataCollectionWizard.Public;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sdk.Messaging;

namespace DataCollectionWizard.Backend.Consumers;

// TODO: add tests
public sealed partial class DeleteOutputConnectorMappingConsumer(IDataCollectionWizardDbContext dbContext, ILogger<DeleteOutputConnectorMappingConsumer> logger) : IConsumer<DeleteOutputConnectorMapping>
{
    public async Task Consume(ConsumeContext<DeleteOutputConnectorMapping> context)
    {
        LogConsume(logger,
            nameof(DeleteDeviceConnectorIds),
            context.CorrelationId,
            context.Message.ProcessDataIds.Count);

        var changes = new List<OutputConnectorMappingChangeItem>();

        foreach (var item in context.Message.ProcessDataIds)
        {
            var existing = await dbContext.ValueMappings
                .FirstOrDefaultAsync(d => d.ProcessDataId == item);

            if (existing is not null)
            {
                dbContext.ValueMappings.Remove(existing);
                changes.Add(new(CrudAction.Deleted, item));
            }
        }

        try
        {
            var dbChanges = await dbContext.Instance.SaveChangesAsync(context.CancellationToken);
            await context.Publish(new OutputConnectorMappingChangedEvent(dbChanges > 0 ? changes : [])).ConfigureAwait(false);
        }
        catch (DbUpdateException e)
        {
            await context.Publish(new OutputConnectorMappingChangedErrorEvent(new ErrorInfo(ErrorCodes.DbUpdateFailed, e.Message)));
        }
    }

    [LoggerMessage(LogLevel.Debug, "Consume {command} CorrelationId:{correlationId} DeviceConnectorIds.Count:{count}")]
    public static partial void LogConsume(ILogger logger, string command, Guid? correlationId, int count);
}
