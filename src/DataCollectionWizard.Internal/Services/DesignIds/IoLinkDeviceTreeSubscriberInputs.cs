namespace DataCollectionWizard.Internal.Services.DesignIds;

public sealed class IoLinkDeviceTreeSubscriberInputs : IDesignIdStore
{
    public static IoLinkDeviceTreeSubscriberInputs Instance { get; } = new IoLinkDeviceTreeSubscriberInputs();

    public Guid Trigger { get; } = Guid.Parse("0ae3d21f-0797-4949-bed9-2e885934753e");
}
