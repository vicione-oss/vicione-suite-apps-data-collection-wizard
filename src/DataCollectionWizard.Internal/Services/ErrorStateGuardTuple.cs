using ViciOne.Cluster.Model;
using ViciOne.Driver.IoTCore.Contracts.DeviceTree;

namespace DataCollectionWizard.Internal.Services;

internal struct ErrorStateGuardTuple
{
    public required ConnectorOutput BlobSensorDataOutput { get; set; }
    public required ConnectorInput BlobSensorErrorStateTriggerInput { get; set; }
    public required EventTrigger Configuration { get; set; }
    public required ConnectorOutput ErrorStateOutput { get; set; }
    public required IDeviceTreeEventTriggerDataNode Sensor { get; set; }
    public required string Unit { get; set; }
}
