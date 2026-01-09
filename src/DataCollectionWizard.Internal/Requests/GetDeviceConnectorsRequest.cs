using Sdk.Messaging;

namespace DataCollectionWizard.Internal.Requests;

public record GetDeviceConnectorsRequest(Uri? DeviceAddress) : IRequest<GetDeviceConnectorsResponse>;
