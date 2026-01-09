using DataCollectionWizard.Internal.Contracts;
using Sdk.Messaging;

namespace DataCollectionWizard.Internal.Requests;

public record GetDeviceConnectorsResponse : IResponse
{
    public List<DeviceConnectorIds> Ids { get; init; } = [];
    public ErrorInfo? RequestError { get; init; }
}
