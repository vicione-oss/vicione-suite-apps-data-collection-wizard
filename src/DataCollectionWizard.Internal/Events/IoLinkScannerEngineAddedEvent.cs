using Sdk.Messaging;

namespace DataCollectionWizard.Internal.Events;

[ForwardToUI]
public record IoLinkScannerEngineAddedEvent(bool ClusterDeployRequired) : IEvent;
