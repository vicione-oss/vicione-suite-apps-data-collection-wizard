using Sdk.Messaging;
using ViciOne.DeviceTree.Contracts;

namespace DataCollectionWizard.Public.Events;

[ForwardToUI]
public sealed record NodesOfflineEvent(IReadOnlyCollection<IDeviceTreeBase> OfflineNodes) : IEvent;
