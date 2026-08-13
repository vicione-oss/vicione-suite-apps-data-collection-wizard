using DataCollectionWizard.Backend.DbContext;
using DataCollectionWizard.Internal.Commands;
using DataCollectionWizard.Internal.Events;
using DataCollectionWizard.Public;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sdk.Messaging;

namespace DataCollectionWizard.Backend.Consumers;

public sealed partial class DeleteDeviceTreeConsumer(IDataCollectionWizardDbContext dbContext, ILogger<DeleteDeviceTreeConsumer> logger) : IConsumer<DeleteDeviceTree>
{
    public async Task Consume(ConsumeContext<DeleteDeviceTree> context)
    {
        LogConsume(logger,
            nameof(DeleteDeviceTree),
            context.CorrelationId,
            context.Message.DeviceAddress);

        var existingDeviceTree = await dbContext.Devices
            .FirstOrDefaultAsync(d => d.DeviceAddress == context.Message.DeviceAddress);

        if (existingDeviceTree is not null)
        {
            dbContext.Devices.Remove(existingDeviceTree);
        }

        try
        {
            if (await dbContext.SaveChangesAsync(context.CancellationToken) > 0)
            {
                await context.Publish(new DeviceTreeChangedEvent(CrudAction.Deleted)).ConfigureAwait(false);
            }
        }
        catch (DbUpdateException e)
        {
            await context.Publish(new DeviceTreeChangeErrorEvent(new ErrorInfo(ErrorCodes.DbUpdateFailed, e.Message)));
        }
    }
}
