using ViciOne.Driver.IoTCore.Contracts.DeviceTree;

namespace DataCollectionWizard.Internal.Services.DeviceDataflowGenerators
{
    public class BlobLoggingConfiguration
    {
        public Guid DataGroupIdentifier { get; set; }
        public bool NeedsEventTrigger { get; set; }
        public bool NeedsScheduler { get; set; }
        public required IDeviceTreeDataNode Node { get; set; }
    }
}
