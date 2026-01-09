using Sdk.Messaging;

namespace DataCollectionWizard.Internal.Events;

[ForwardToUI]
public sealed record DeviceConnectorIdsChangeErrorEvent(ErrorInfo Error) : IEvent;
