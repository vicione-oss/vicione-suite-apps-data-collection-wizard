using Sdk.Messaging;
using ViciOne.Driver.IoTCore.Contracts.DeviceTree;

namespace DataCollectionWizard.Public.Events;

[ForwardToUI]
public sealed record NodesOnlineEvent(IReadOnlyCollection<IDeviceTreeBase> OnlineNodes) : IEvent;
