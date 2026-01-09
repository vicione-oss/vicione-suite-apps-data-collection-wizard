namespace DataCollectionWizard.Internal.Services.DesignIds;

public interface IIoLinkSubscriberSettings
{
    Guid Address { get; }
    Guid ApplicationSpecificTag { get; }
    Guid DeviceId { get; }
    Guid PortIndex { get; }
    Guid ProcessDataInIndex { get; }
    Guid ProductName { get; }
    Guid VendorId { get; }
}
