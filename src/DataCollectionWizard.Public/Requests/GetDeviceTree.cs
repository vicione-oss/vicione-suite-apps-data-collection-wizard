using Sdk.Messaging;

namespace DataCollectionWizard.Public.Requests;

public record GetDeviceTree(bool FakeData = false) : IRequest<GetDeviceTreeResponse>;
