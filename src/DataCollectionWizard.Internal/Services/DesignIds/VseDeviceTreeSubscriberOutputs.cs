namespace DataCollectionWizard.Internal.Services.DesignIds;

public sealed class VseDeviceTreeSubscriberOutputs : IDesignIdStore
{
    public static VseDeviceTreeSubscriberOutputs Instance { get; } = new VseDeviceTreeSubscriberOutputs();

    public Guid DeviceTree { get; } = Guid.Parse("74010ddc-7f1d-4bf4-895f-237225a77329");
}
