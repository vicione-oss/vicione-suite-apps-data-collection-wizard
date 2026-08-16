namespace DataCollectionWizard.Internal.Services.DesignIds;

public sealed class IoLinkStringSubscriberSettings : IDesignIdStore, IIoLinkSubscriberSettings
{
    public static IoLinkStringSubscriberSettings Instance { get; } = new IoLinkStringSubscriberSettings();

    public Guid Identifier { get; } = Guid.Parse("9f4fcc89-cc14-4180-b334-eac411ae1cbd");
    public Guid DeviceId { get; } = Guid.Parse("4ce703e9-6199-4837-afc5-1c49f76a8767");
    public Guid PortIndex { get; } = Guid.Parse("a9813049-46e9-4d78-a5e2-d9fe4bd97c95");
    public Guid ProcessDataInIndex { get; } = Guid.Parse("439567dc-06cd-4b5a-80a1-cd1a49c2174b");
    public Guid VendorId { get; } = Guid.Parse("d73c2bf5-3a09-478c-b2f0-ba5e4ff5ad5b");
}
