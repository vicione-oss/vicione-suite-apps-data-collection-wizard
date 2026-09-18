namespace DataCollectionWizard.Internal.Services.DesignIds;

public sealed class IoLinkDoubleSubscriberSettings : IDesignIdStore, IIoLinkSubscriberSettings
{
    public static IoLinkDoubleSubscriberSettings Instance { get; } = new IoLinkDoubleSubscriberSettings();

    public Guid Identifier { get; } = Guid.Parse("8418803c-e3db-413d-9c33-d0e5a7da8a74");
    public Guid DeviceId { get; } = Guid.Parse("2cec8a97-b943-42b9-a722-f58223f45552");
    public Guid PortIndex { get; } = Guid.Parse("154e2818-8646-4d75-9ee8-95c5a21dbc1c");
    public Guid ProcessDataInIndex { get; } = Guid.Parse("e284ee60-67d9-4d0d-ade6-e9227cbbd090");
    public Guid VendorId { get; } = Guid.Parse("22ae0260-0325-4c79-bfaa-c8206d2dec80");
}
