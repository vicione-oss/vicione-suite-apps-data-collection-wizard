using DataCollectionWizard.Internal.Services.DeviceDataflowGenerators;
using Sdk.Connections.Contracts;
using ViciOne.Cluster.Builder;
using ViciOne.Cluster.Model;
using ViciOne.DeviceTree.Contracts;

namespace DataCollectionWizard.Internal.Services.CloudDataflowGenerators;

public interface ICloudDataflowGenerator
{
    string Name { get; }

    Dictionary<string, AggregationFunctionCloudInputs> GenerateCloudDataflow(Connection connection, IDeviceTreeMasterNode deviceTreeMaster, ClusterBuilder builder,
                                                                     Dataflow dataflow, string machineIdentifier, Dictionary<string, DataOutputInfo> dataOutputs, uint engineCycleInterval,
                                                                     ChildContainer cloudContainer, Dictionary<string, RotationalFrequencyOutputs> rotationalFrequencyOutputs,
                                                                     List<ProcessDataConfiguration> loggedProcessDataNodes, List<IDeviceTreeDataNode> loggedRawDataNodes);
}
