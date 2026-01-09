using DataCollectionWizard.Internal.Contracts;
using Sdk.Connections.Contracts;
using ViciOne.Cluster.Model;
using ViciOne.Driver.IoTCore.Contracts.DeviceTree;

namespace DataCollectionWizard.Internal.Services;

public interface IDataflowGenerator
{
    int ContainerSize { get; set; }

    void Generate(IDeviceTreeMasterNode master, IReadOnlyCollection<Connection> publishTargets, Dataflow dataflow, Engine engine, out Guid deviceTreeTrigger, out Guid deviceTreeOutput, out List<ValueMappingEntry> outputMapping);
}
