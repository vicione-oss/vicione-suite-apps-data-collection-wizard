using ViciOne.Driver.IoTCore.Contracts.DeviceTree;

namespace DataCollectionWizard.Internal.Services.CloudDataflowGenerators;

public record ProcessDataConfiguration(IDeviceTreeDataNode Node, CompressorConfiguration Configuration);
