using ViciOne.Cluster.Builder;
using ViciOne.Cluster.Model;
using ViciOne.DeviceTree.Contracts;

namespace DataCollectionWizard.Internal.Services.DeviceDataflowGenerators;

public interface IDeviceDataflowGenerator
{
    Type DeviceType { get; }

    DeviceDataflowGeneratorResult GenerateDeviceFunctionBlocks(ClusterBuilder builder, Dataflow dataflow, IDeviceTreeMasterNode device, Dictionary<string, bool> enabledDataIds,
        Dictionary<Guid, string> cloudNames, BlobLoggingConfiguration[] blobLoggingConfigurations, string connectionIdentifier);

    DeviceTreeFunctionBlockResult GenerateDeviceTreeSourceFunctionBlock(ClusterBuilder builder, Dataflow dataflow, string address, string connectionIdentifier);
}
