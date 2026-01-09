using ViciOne.Cluster.Builder;
using ViciOne.Cluster.Model;
using ViciOne.Driver.IoTCore.Contracts.DeviceTree;

namespace DataCollectionWizard.Internal.Services.DeviceDataflowGenerators;

public interface IDeviceDataflowGenerator
{
    Type DeviceType { get; }

    DeviceDataflowGeneratorResult GenerateDeviceFunctionBlocks(ClusterBuilder builder, Dataflow dataflow, IDeviceTreeMasterNode device, Dictionary<string, bool> enabledDataIds,
        Dictionary<Guid, string> cloudNames, BlobLoggingConfiguration[] blobLoggingConfigurations);

    DeviceTreeFunctionblockResult GenerateGetDeviceTreeFunctionblock(ClusterBuilder builder, Dataflow dataflow, string address);
}
