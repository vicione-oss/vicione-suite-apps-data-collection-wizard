using Microsoft.Extensions.Logging;
using Sdk.Messaging;
using ViciOne.Driver.IoTCore.Contracts.DeviceTree;

namespace DataCollectionWizard.Internal.Commands;

public record UpdateDeviceTree(Guid Token, DeviceTreeRoot DeviceTree, IReadOnlyCollection<IDeviceTreeBase> DeletedNodes, IReadOnlyCollection<string> MasterNodesToUpdate, LogLevel? LogLevel) : ICommand
{
    public Guid CorrelationId { get; init; } = Guid.NewGuid();
}
