namespace DataCollectionWizard.Internal.Services.DesignIds;

public sealed class IoTCoreConfigurationInputs : IDesignIdStore
{
    public static IoTCoreConfigurationInputs Instance { get; } = new IoTCoreConfigurationInputs();

    public Guid RebuildDeviceTree { get; } = Guid.Parse("0ae3d21f-0797-4949-bed9-2e885934753e");
}
