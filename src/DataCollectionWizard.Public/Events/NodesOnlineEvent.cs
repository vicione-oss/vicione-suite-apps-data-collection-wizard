using Sdk.Messaging;
using ViciOne.DeviceTree.Contracts;

namespace DataCollectionWizard.Public.Events;

[ForwardToUI]
public sealed record NodesOnlineEvent(IReadOnlyCollection<IDeviceTreeBase> OnlineNodes) : IEvent;
