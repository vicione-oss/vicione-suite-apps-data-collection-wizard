using Sdk.Messaging;

namespace DataCollectionWizard.Internal.Events;

[ForwardToUI]
public record DeviceScannerEngineAddedEvent(bool ClusterDeployRequired) : IEvent;
