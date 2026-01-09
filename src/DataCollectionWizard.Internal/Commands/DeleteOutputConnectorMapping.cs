using Sdk.Messaging;

namespace DataCollectionWizard.Internal.Commands;

public record DeleteOutputConnectorMapping(List<string> ProcessDataIds) : ICommand
{
    public Guid CorrelationId { get; init; } = Guid.NewGuid();
}
