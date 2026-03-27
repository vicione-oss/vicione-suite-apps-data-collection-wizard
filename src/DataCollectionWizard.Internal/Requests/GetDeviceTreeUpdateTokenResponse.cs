using Sdk.Messaging;

namespace DataCollectionWizard.Internal.Requests;

public class GetDeviceTreeUpdateTokenResponse : IResponse
{
    public ErrorInfo? RequestError { get; init; }
    public Guid Token { get; set; }
}
