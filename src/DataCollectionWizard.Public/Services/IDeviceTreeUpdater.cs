using Microsoft.Extensions.Logging;
using ViciOne.Driver.IoTCore.Contracts.DeviceTree;

namespace DataCollectionWizard.Public.Services;

public interface IDeviceTreeUpdater
{
    Task<Guid> RequestUpdateAsync(CancellationToken cancellationToken, Guid? correlationId = null, TimeSpan? validity = null);

    Task<DeviceTreeRoot> LoadDeviceTree(CancellationToken? cancellationToken = null);

    Task UpdateDeviceTreeAsync(Guid ticketId,
        DeviceTreeRoot deviceTree,
        IDeviceTreeBase[] deletedNodes,
        IEnumerable<string> masterNodesToUpdate,
        LogLevel? logLevel = null,
        bool saveTree = true,
        CancellationToken? cancellationToken = null);

    void DiscardUpdateRequest(Guid ticketId);
}
