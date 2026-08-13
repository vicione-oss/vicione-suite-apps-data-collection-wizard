using ViciOne.DeviceTree.Contracts;

namespace DataCollectionWizard.Internal.Services.CloudDataflowGenerators;

public record ProcessDataConfiguration(IDeviceTreeDataNode Node, CompressorConfiguration Configuration);
