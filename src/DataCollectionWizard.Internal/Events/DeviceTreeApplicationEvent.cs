using MassTransit;
using Sdk.Messaging;

namespace DataCollectionWizard.Internal.Events;

[ForwardToUI]
public sealed record DeviceTreeApplicationEvent(ErrorInfo? ErrorInfo = null) : IEvent, CorrelatedBy<Guid>
{
    public bool WasSuccessful => ErrorInfo is null;
    public Guid CorrelationId { get; init; }
}
