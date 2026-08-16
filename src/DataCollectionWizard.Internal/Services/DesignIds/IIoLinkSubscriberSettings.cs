namespace DataCollectionWizard.Internal.Services.DesignIds;

public interface IIoLinkSubscriberSettings
{
    Guid Identifier { get; }
    Guid DeviceId { get; }
    Guid PortIndex { get; }
    Guid ProcessDataInIndex { get; }
    Guid VendorId { get; }
}
