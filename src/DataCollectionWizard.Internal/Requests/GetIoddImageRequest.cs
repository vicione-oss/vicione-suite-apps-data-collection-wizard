using Sdk.Messaging;

namespace DataCollectionWizard.Internal.Requests;

public record GetIoddImageRequest(ushort VendorId, uint DeviceId, string ImageFileName) : IRequest<GetIoddImageResponse>;
