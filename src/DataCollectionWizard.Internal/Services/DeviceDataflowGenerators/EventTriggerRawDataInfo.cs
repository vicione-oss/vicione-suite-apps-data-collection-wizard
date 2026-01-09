using ViciOne.Cluster.Model;

namespace DataCollectionWizard.Internal.Services.DeviceDataflowGenerators;

public class EventTriggerRawDataInfo
{
    public required ConnectorInput EventTriggerInput { get; set; }
    public required ConnectorOutput MeasurementOutput { get; set; }
}
