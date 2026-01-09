using Sdk.Messaging;

namespace DataCollectionWizard.Internal.Events;

[ForwardToUI]
public sealed record DeviceTreeChangeErrorEvent(ErrorInfo Error) : IEvent;
