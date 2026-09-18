using DataCollectionWizard.Backend.Services;
using MassTransit;
using Sdk.Connections.Events;

namespace DataCollectionWizard.Backend.Consumers;

public sealed class ConnectionChangedConsumer(IConnectionChangedProcessor connectionChangedProcessor) : IConsumer<ConnectionChanged>
{
    public Task Consume(ConsumeContext<ConnectionChanged> context)
    {
        connectionChangedProcessor.Enqueue(context.Message);
        return Task.CompletedTask;
    }
}
