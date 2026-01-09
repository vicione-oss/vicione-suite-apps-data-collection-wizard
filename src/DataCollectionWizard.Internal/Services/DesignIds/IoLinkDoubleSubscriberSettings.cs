namespace DataCollectionWizard.Internal.Services.DesignIds;

public sealed class IoLinkDoubleSubscriberSettings : IDesignIdStore, IIoLinkSubscriberSettings
{
    public static IoLinkDoubleSubscriberSettings Instance { get; } = new IoLinkDoubleSubscriberSettings();

    public Guid Address { get; } = Guid.Parse("8418803c-e3db-413d-9c33-d0e5a7da8a74");
    public Guid ApplicationSpecificTag { get; } = Guid.Parse("47c6e8a6-7537-4bfd-943d-204ea757e61e");
    public Guid DeviceId { get; } = Guid.Parse("2cec8a97-b943-42b9-a722-f58223f45552");
    public Guid PortIndex { get; } = Guid.Parse("154e2818-8646-4d75-9ee8-95c5a21dbc1c");
    public Guid ProcessDataInIndex { get; } = Guid.Parse("e284ee60-67d9-4d0d-ade6-e9227cbbd090");
    public Guid ProductName { get; } = Guid.Parse("b199a673-92e3-424f-9c60-50cab0824865");
    public Guid VendorId { get; } = Guid.Parse("22ae0260-0325-4c79-bfaa-c8206d2dec80");
}
