namespace DataCollectionWizard.Internal.Services.DesignIds;

public sealed class IoTCoreConfigurationOutputs : IDesignIdStore
{
    public static IoTCoreConfigurationOutputs Instance { get; } = new IoTCoreConfigurationOutputs();

    public Guid DeviceTree { get; } = Guid.Parse("9105930c-9fb9-4a24-9db9-bebfd5ab1960");
    public Guid IoddDirectory { get; } = Guid.Parse("34e64b82-0a49-4ed6-9d97-01ab020b2ecc");
}
