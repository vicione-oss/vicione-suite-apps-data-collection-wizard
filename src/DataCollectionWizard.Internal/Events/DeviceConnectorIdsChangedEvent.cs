using DataCollectionWizard.Internal.Contracts;
using Sdk.Messaging;

namespace DataCollectionWizard.Internal.Events;

[ForwardToUI]
public sealed record DeviceConnectorIdsChangedEvent(List<DeviceConnectorIdsChangeItem> ChangedItems) : IEvent;

public sealed record DeviceConnectorIdsChangeItem(CrudAction Action, DeviceConnectorIds Ids);
