using ViciOne.Cluster.Model;

namespace DataCollectionWizard.Internal.Services.DeviceDataflowGenerators;

public class ScheduledRawDataInfo
{
    public required ConnectorOutput MeasurementOutput { get; set; }
    public required ConnectorInput TriggerInput { get; set; }
}
