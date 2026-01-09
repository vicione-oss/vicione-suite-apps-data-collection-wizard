using DataCollectionWizard.Internal.Contracts;
using ViciOne.Cluster.Model;

namespace DataCollectionWizard.Internal.Services.DeviceDataflowGenerators;

public class DeviceDataflowGeneratorResult
{
    public Dictionary<string, DataOutputInfo> DataOutputs { get; } = [];
    public Dictionary<string, ConnectorOutput> ErrorStateOutputs { get; } = [];
    public List<ValueMappingEntry> OutputMapping { get; } = [];
    public Dictionary<string, RawDataInfo> RawData { get; } = [];
    public ChildContainer? RawDataContainer { get; set; }
    public Dictionary<string, RotationalFrequencyOutputs> RotationalFrequencyOutputs { get; } = [];
}
