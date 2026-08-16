using Sdk.Messaging;

namespace DataCollectionWizard.Internal.Events;

[ForwardToUI]
public sealed record DeviceConnectorIdsChangedErrorEvent(ErrorInfo Error) : IEvent;
