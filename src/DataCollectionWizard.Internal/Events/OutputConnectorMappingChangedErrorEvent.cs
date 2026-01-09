using Sdk.Messaging;

namespace DataCollectionWizard.Internal.Events;

[ForwardToUI]
public sealed record OutputConnectorMappingChangedErrorEvent(ErrorInfo Error) : IEvent;
