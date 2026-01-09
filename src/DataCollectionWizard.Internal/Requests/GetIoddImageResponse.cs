using Sdk.Messaging;

namespace DataCollectionWizard.Internal.Requests;

public record GetIoddImageResponse : IResponse
{
    public required string ImageDataBase64 { get; init; }
    public ErrorInfo? RequestError { get; init; }
}
