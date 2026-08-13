using Sdk.Messaging;
using ViciOne.DeviceTree.Contracts;

namespace DataCollectionWizard.Public.Requests;

public record GetDeviceTreeResponse : IResponse
{
    public required DeviceTreeRoot DeviceTree { get; init; }
    public ErrorInfo? RequestError { get; init; }
}
