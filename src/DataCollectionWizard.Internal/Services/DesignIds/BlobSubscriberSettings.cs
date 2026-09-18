namespace DataCollectionWizard.Internal.Services.DesignIds;

public sealed class BlobSubscriberSettings : IDesignIdStore
{
    public static BlobSubscriberSettings Instance { get; } = new BlobSubscriberSettings();

    public Guid Identifier { get; } = Guid.Parse("25229950-3fd8-4a80-95b7-9d6534215de0");
    public Guid DeviceId { get; } = Guid.Parse("46438cbc-46d3-4d24-8b49-c5b1188cee24");
    public Guid IgnoreTimedValueArray { get; } = Guid.Parse("1788008d-a651-4941-92d5-22b338d37249");
    public Guid PortIndex { get; } = Guid.Parse("22ee0a83-1503-4699-8a4c-c0cbd9383db5");
    public Guid ProductName { get; } = Guid.Parse("cb484a49-0d1c-411f-84fe-36e02f289fc4");
    public Guid VendorId { get; } = Guid.Parse("b4724efb-7f33-415f-ae9e-17e6045d41e8");
}
