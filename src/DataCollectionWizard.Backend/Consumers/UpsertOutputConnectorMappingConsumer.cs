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
public sealed partial class UpsertOutputConnectorMappingConsumer(IDataCollectionWizardDbContext dbContext, ILogger<UpsertOutputConnectorMappingConsumer> logger) : IConsumer<UpsertOutputConnectorMapping>
{
    public async Task Consume(ConsumeContext<UpsertOutputConnectorMapping> context)
    {
        LogConsume(logger,
            nameof(UpsertOutputConnectorMapping),
            context.CorrelationId,
            context.Message.ValueMappingEntries.Count);

        var changes = new List<OutputConnectorMappingChangeItem>();

        foreach (var item in context.Message.ValueMappingEntries)
        {
            var existing = await dbContext.ValueMappings
                .FirstOrDefaultAsync(d => d.ProcessDataId == item.ProcessDataId);

            if (existing is null)
            {
                dbContext.ValueMappings.Add(item);
                changes.Add(new OutputConnectorMappingChangeItem(CrudAction.Created, item.ProcessDataId));
            }
            else
            {
                dbContext.ValueMappings.Remove(existing);
                dbContext.ValueMappings.Add(item);
                changes.Add(new OutputConnectorMappingChangeItem(CrudAction.Updated, item.ProcessDataId));
            }
        }

        try
        {
            var dbChanges = await dbContext.SaveChangesAsync(context.CancellationToken);
            await context.Publish(new OutputConnectorMappingChangedEvent(dbChanges > 0 ? changes : [])).ConfigureAwait(false);
        }
        catch (DbUpdateException e)
        {
            await context.Publish(new OutputConnectorMappingChangedErrorEvent(new ErrorInfo(ErrorCodes.DbUpdateFailed, e.Message)));
        }
    }
}
