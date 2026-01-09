using Sdk.Messaging;
using ViciOne.Driver.IoTCore.Contracts.DeviceTree;

namespace DataCollectionWizard.Public.Requests;

public record GetDeviceTreeResponse : IResponse
{
    public required DeviceTreeRoot DeviceTree { get; init; }
    public ErrorInfo? RequestError { get; init; }
}
