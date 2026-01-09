using Sdk.Messaging;

namespace DataCollectionWizard.Internal.Events;

[ForwardToUI]
public sealed record DeviceTreeChangedEvent(CrudAction Action) : IEvent;
