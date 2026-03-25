using DataCollectionWizard.Internal.Commands;
using DataCollectionWizard.Public.Services;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace DataCollectionWizard.Backend.Consumers;

public sealed partial class UpdateDeviceTreeConsumer(IDeviceTreeUpdater updater, ILogger<UpsertDeviceConnectorIdsConsumer> logger) : IConsumer<UpdateDeviceTree>
{
    public async Task Consume(ConsumeContext<UpdateDeviceTree> context)
    {
        LogConsume(logger,
            nameof(UpsertDeviceConnectorIds),
            context.CorrelationId);

        await updater.UpdateDeviceTreeAsync(context.Message.Token, context.Message.DeviceTree, context.Message.DeletedNodes.ToArray(), context.Message.MasterNodesToUpdate, context.Message.LogLevel);
    }

    [LoggerMessage(LogLevel.Debug, "Consume {command} CorrelationId:{correlationId}")]
    public static partial void LogConsume(ILogger logger, string command, Guid? correlationId);
}
