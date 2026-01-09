
using Sdk.Messaging;

namespace DataCollectionWizard.Internal.Commands;

public record DeleteDeviceTree(string DeviceAddress) : ICommand
{
    public Guid CorrelationId { get; init; } = Guid.NewGuid();
}
