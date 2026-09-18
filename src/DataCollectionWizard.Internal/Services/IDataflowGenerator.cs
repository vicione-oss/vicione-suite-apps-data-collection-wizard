using DataCollectionWizard.Internal.Contracts;
using Sdk.Connections.Contracts;
using ViciOne.Cluster.Model;
using ViciOne.DeviceTree.Contracts;

namespace DataCollectionWizard.Internal.Services;

internal interface IDataflowGenerator
{
    void Generate(IDeviceTreeMasterNode master, IReadOnlyCollection<Connection> publishTargets, Dataflow dataflow, Engine engine, out Guid deviceTreeTrigger, out Guid deviceTreeOutput, out List<ValueMappingEntry> outputMapping);
}
