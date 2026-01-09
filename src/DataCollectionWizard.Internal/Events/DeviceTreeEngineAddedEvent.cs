using DataCollectionWizard.Internal.Contracts;
using MassTransit;
using Sdk.Messaging;

namespace DataCollectionWizard.Internal.Events;

[ForwardToUI]
public record DeviceTreeEngineAddedEvent : IEvent, CorrelatedBy<Guid>
{
    public required Uri Address { get; init; }
    public required DeviceConnectorIds DeviceTreeConnectors { get; init; }
    public Guid CorrelationId { get; set; }
    public ErrorInfo? RequestError { get; init; }
}
