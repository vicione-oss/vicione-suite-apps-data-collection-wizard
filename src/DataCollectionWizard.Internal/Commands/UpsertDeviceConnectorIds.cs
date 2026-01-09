using DataCollectionWizard.Internal.Contracts;
using Sdk.Messaging;

namespace DataCollectionWizard.Internal.Commands;

public record UpsertDeviceConnectorIds(List<DeviceConnectorIds> DeviceConnectorIds) : ICommand
{
    public Guid CorrelationId { get; init; } = Guid.NewGuid();
}
