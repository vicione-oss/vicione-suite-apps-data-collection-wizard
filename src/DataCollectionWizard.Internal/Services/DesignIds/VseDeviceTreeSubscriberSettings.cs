namespace DataCollectionWizard.Internal.Services.DesignIds;

public sealed class VseDeviceTreeSubscriberSettings : IDesignIdStore
{
    public static VseDeviceTreeSubscriberSettings Instance { get; } = new VseDeviceTreeSubscriberSettings();

    public Guid Url { get; } = Guid.Parse("64b80988-b76e-41e9-ba47-8f0884b0e90e");
}
