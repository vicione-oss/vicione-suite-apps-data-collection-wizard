namespace DataCollectionWizard.Internal.Services.DesignIds;

public sealed class IoLinkBooleanSubscriberSettings : IDesignIdStore, IIoLinkSubscriberSettings
{
    public static IoLinkBooleanSubscriberSettings Instance { get; } = new IoLinkBooleanSubscriberSettings();

    public Guid Address { get; } = Guid.Parse("1952d10d-df1d-4d78-8d62-71b38c6f13f0");
    public Guid ApplicationSpecificTag { get; } = Guid.Parse("8129883e-5df3-429b-8d9c-7b18f191f381");
    public Guid DeviceId { get; } = Guid.Parse("9ce0c188-1f51-423c-9e21-de9da593dcf7");
    public Guid PortIndex { get; } = Guid.Parse("8eb74be8-63a6-470c-8eca-3b8fcedcd7a7");
    public Guid ProcessDataInIndex { get; } = Guid.Parse("319e6bd0-0369-4852-b5e7-e6a539e35668");
    public Guid ProductName { get; } = Guid.Parse("06e858fb-f9ff-4b89-859b-6063898acba7");
    public Guid VendorId { get; } = Guid.Parse("d3c2f6e2-9f4c-4153-88ef-91b33bf3326a");
}
