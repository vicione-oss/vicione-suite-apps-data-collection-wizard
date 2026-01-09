using ViciOne.Cluster.Model;

namespace DataCollectionWizard.Internal.Services.DeviceDataflowGenerators;

public class RotationalFrequencyOutputs
{
    public required ConnectorOutput RefValue { get; set; }
    public required ConnectorOutput RotationalFrequencyTuple { get; set; }
    public required ConnectorOutput RotSpeed { get; set; }
}
