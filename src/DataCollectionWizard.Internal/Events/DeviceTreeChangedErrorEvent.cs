using Sdk.Messaging;

namespace DataCollectionWizard.Internal.Events;

[ForwardToUI]
public sealed record DeviceTreeChangedErrorEvent(ErrorInfo Error) : IEvent;
