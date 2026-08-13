using ViciOne.Cluster.Model;
using ViciOne.DeviceTree.Contracts;

namespace DataCollectionWizard.Internal.Services;

internal struct ErrorStateGuardTuple
{
    public required ConnectorOutput BlobSensorDataOutput { get; set; }
    public required ConnectorInput BlobSensorErrorStateTriggerInput { get; set; }
    public required EventTrigger Configuration { get; set; }
    public required ConnectorOutput ErrorStateOutput { get; set; }
    public required IDeviceTreeEventTriggerDataNode Sensor { get; set; }
}
