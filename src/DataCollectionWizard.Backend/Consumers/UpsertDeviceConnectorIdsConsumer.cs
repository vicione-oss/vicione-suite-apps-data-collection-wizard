using DataCollectionWizard.Backend.DbContext;
using DataCollectionWizard.Internal.Commands;
using DataCollectionWizard.Internal.Events;
using DataCollectionWizard.Public;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sdk.Messaging;

namespace DataCollectionWizard.Backend.Consumers;

public sealed partial class UpsertDeviceConnectorIdsConsumer(IDataCollectionWizardDbContext dbContext, ILogger<UpsertDeviceConnectorIdsConsumer> logger) : IConsumer<UpsertDeviceConnectorIds>
{
    public async Task Consume(ConsumeContext<UpsertDeviceConnectorIds> context)
    {
        LogConsume(logger,
            nameof(UpsertDeviceConnectorIds),
            context.CorrelationId,
            context.Message.DeviceConnectorIds.Count);

        var changes = new List<DeviceConnectorIdsChangeItem>();

        foreach (var item in context.Message.DeviceConnectorIds)
        {
            var action = CrudAction.Created;
            var existing = await dbContext.DeviceConnectorIds
                .FirstOrDefaultAsync(d => d.DeviceAddress == item.DeviceAddress);

            if (existing is not null)
            {
                if (existing.TriggerInput == item.TriggerInput && existing.DeviceTreeOutput == item.DeviceTreeOutput)
                {
                    continue;
                }

                dbContext.DeviceConnectorIds.Remove(existing);
                action = CrudAction.Updated;
            }

            dbContext.DeviceConnectorIds.Add(item);

            changes.Add(new DeviceConnectorIdsChangeItem(action, item));
        }

        try
        {
            var dbChanges = await dbContext.SaveChangesAsync(context.CancellationToken);

            await context.Publish(new DeviceConnectorIdsChangedEvent(dbChanges > 0 ? changes : [])).ConfigureAwait(false);
        }
        catch (DbUpdateException e)
        {
            await context.Publish(new DeviceConnectorIdsChangedErrorEvent(new ErrorInfo(ErrorCodes.DbUpdateFailed, e.Message)));
        }
    }
}
