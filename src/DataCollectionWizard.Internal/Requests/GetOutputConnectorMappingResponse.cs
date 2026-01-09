using DataCollectionWizard.Internal.Contracts;
using Sdk.Messaging;

namespace DataCollectionWizard.Internal.Requests;

public record GetOutputConnectorMappingResponse : IResponse
{
    public List<ValueMappingEntry> Mapping { get; init; } = [];
    public ErrorInfo? RequestError { get; init; }
}
