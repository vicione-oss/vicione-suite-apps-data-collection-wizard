using Sdk.Messaging;

namespace DataCollectionWizard.Internal.Requests;

public record GetDevicesResponse : IResponse
{
    public List<string> Devices { get; init; } = [];
    public ErrorInfo? RequestError { get; init; }
}
