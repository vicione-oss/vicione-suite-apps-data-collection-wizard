using Sdk.Messaging;

namespace DataCollectionWizard.Internal.Events;

[ForwardToUI]
public sealed record OutputConnectorMappingChangedEvent(List<OutputConnectorMappingChangeItem> ChangedItems) : IEvent;

public sealed record OutputConnectorMappingChangeItem(CrudAction Action, string ProcessDataId);
