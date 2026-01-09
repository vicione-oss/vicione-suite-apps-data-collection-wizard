using Sdk.Messaging;
using ViciOne.Driver.IoTCore.Contracts.DeviceTree;

namespace DataCollectionWizard.Public.Events;

[ForwardToUI]
public sealed record NodesOfflineEvent(IReadOnlyCollection<IDeviceTreeBase> OfflineNodes) : IEvent;
