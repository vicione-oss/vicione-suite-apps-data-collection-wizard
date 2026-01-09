namespace DataCollectionWizard.Internal.Services.DesignIds;

public sealed class IoLinkDeviceTreeSubscriberSettings : IDesignIdStore
{
    public static IoLinkDeviceTreeSubscriberSettings Instance { get; } = new IoLinkDeviceTreeSubscriberSettings();

    public Guid IoddAutoDownload { get; } = Guid.Parse("b556fca2-9ede-4a4b-a853-e58cb31acfcf");
    public Guid IoddDirectory { get; } = Guid.Parse("dc73e1e1-1588-4269-88a4-1c06ec943c30");
    public Guid Url { get; } = Guid.Parse("588d363b-0823-47c9-adf7-37db039ac408");
}
