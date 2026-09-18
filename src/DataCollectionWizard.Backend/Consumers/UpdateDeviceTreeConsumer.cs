using DataCollectionWizard.Internal.Commands;
using DataCollectionWizard.Public.Services;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace DataCollectionWizard.Backend.Consumers;

public sealed partial class UpdateDeviceTreeConsumer(IDeviceTreeUpdater updater, ILogger<UpdateDeviceTreeConsumer> logger) : IConsumer<UpdateDeviceTree>
{
    public async Task Consume(ConsumeContext<UpdateDeviceTree> context)
    {
        LogConsume(logger,
            nameof(UpdateDeviceTree),
            context.CorrelationId);

        await updater.UpdateDeviceTreeAsync(context.Message.Token, context.Message.DeviceTree, [.. context.Message.DeletedNodes], context.Message.MasterNodesToUpdate, context.Message.LogLevel);
    }
}
