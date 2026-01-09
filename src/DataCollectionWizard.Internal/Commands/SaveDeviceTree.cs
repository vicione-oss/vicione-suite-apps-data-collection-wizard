using Sdk.Messaging;
using ViciOne.Driver.IoTCore.Contracts.DeviceTree;

namespace DataCollectionWizard.Internal.Commands;

public record SaveDeviceTree(DeviceTreeRoot DeviceTree) : ICommand
{
    public Guid CorrelationId { get; init; } = Guid.NewGuid();
}
