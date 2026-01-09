namespace DataCollectionWizard.Internal.Services.DesignIds;

public sealed class IoLinkDeviceTreeSubscriberOutputs : IDesignIdStore
{
    public static IoLinkDeviceTreeSubscriberOutputs Instance { get; } = new IoLinkDeviceTreeSubscriberOutputs();

    public Guid DeviceTree { get; } = Guid.Parse("9105930c-9fb9-4a24-9db9-bebfd5ab1960");
}
