using DataCollectionWizard.Internal.Contracts;
using Sdk.Messaging;

namespace DataCollectionWizard.Internal.Commands;

public record UpsertOutputConnectorMapping(List<ValueMappingEntry> ValueMappingEntries) : ICommand
{
    public Guid CorrelationId { get; init; } = Guid.NewGuid();
}
