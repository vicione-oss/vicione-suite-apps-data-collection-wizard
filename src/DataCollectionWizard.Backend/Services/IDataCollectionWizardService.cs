using DataCollectionWizard.Internal.Commands;
using Microsoft.Extensions.Logging;
using ViciOne.Cluster.Model;
using ViciOne.DeviceTree.Contracts;

namespace DataCollectionWizard.Backend.Services;

public interface IDataCollectionWizardService
{
    Task<Cluster?> AddDeviceTreeEnginesAsync(IEnumerable<DeviceEngineInfo> deviceEngineInfos, Guid correlationId, bool allowUseExistingEngine, LogLevel logLevel);
    Task<Cluster?> AddDeviceScannerAsync(LogLevel logLevel);
    Task<Cluster> ApplyDeviceTreeAsync(IEnumerable<string> masterNodesToUpdate,
                              IDeviceTreeBase[] deletedNodes,
                              DeviceTreeRoot deviceTree,
                              LogLevel? logLevel,
                              CancellationToken cancellationToken);
    Task<DeviceTreeRoot> RequestDeviceTreeAsync(CancellationToken cancellationToken);
}
