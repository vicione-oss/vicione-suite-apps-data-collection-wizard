using DataCollectionWizard.Internal.Commands;
using DataCollectionWizard.Internal.Contracts;
using Microsoft.Extensions.Logging;
using ViciOne.DeviceTree.Contracts;
using ViciOne.DeviceTree.Contracts.Scanning;

namespace DataCollectionWizard.Client.Services;

public interface IDataCollectionWizardService : IDisposable
{
    event Action<bool>? DataPossiblyInvalid;
    event Func<string[], Task> NodesOffline;
    event Func<string[], Task> NodesOnline;

    Task<bool> AddIoLinkScannerDataflow(LogLevel logLevel);
    Task<List<ValueMappingEntry>> GetOutputConnectorMappingAsync();
    Task<bool> IsClusterRunningAsync();
    Task<bool> IsDeployInProgressAsync();
    Task RequestExistingDeviceAsync(Type type, Uri deviceAddress, bool triggerSubscriber, Func<IDeviceTreeMasterNode?, bool, Uri, Task> callback);
    Task RequestExistingDeviceAsync<T>(Uri deviceAddress, bool triggerSubscriber, Func<IDeviceTreeMasterNode?, bool, Uri, Task> callback) where T : IDeviceTreeMasterNode;
    Task RequestExistingDevicesAsync(IEnumerable<IDeviceTreeMasterNode> devices, bool triggerSubscriber, Func<IReadOnlyCollection<(IDeviceTreeMasterNode? device, Uri address, bool success)>, Task> callback);
    Task RequestNewDeviceDeviceTreeAsync(Type deviceType, Uri address, Func<IDeviceTreeMasterNode?, bool, Uri, Task> callBack, bool allowUseExistingEngine, LogLevel logLevel);
    Task RequestNewDevicesDeviceTreeAsync(IEnumerable<DeviceEngineInfo> deviceEngineInfos, Func<IDeviceTreeMasterNode?, bool, Uri, Task> callBack, bool allowUseExistingEngine, LogLevel logLevel);
    Task<DeviceTreeRoot> RequestDeviceTreeAsync();
    Task SaveDeviceTreeAsync(IReadOnlyCollection<string> masterNodesToUpdate,
                             IReadOnlyCollection<IDeviceTreeBase> deletedNodes,
                             DeviceTreeRoot deviceTree, LogLevel logLevel);
    Task<DcpScanningResult> ScanIoLinkDevicesAsync(LogLevel logLevel, CancellationToken cancellationToken);
    Task<bool> WaitForCurrentDeployment(TimeSpan timeout);
}
