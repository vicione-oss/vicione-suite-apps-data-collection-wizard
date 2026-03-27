using DataCollectionWizard.Internal.Requests;
using DataCollectionWizard.Public;
using DataCollectionWizard.Public.Services;
using Sdk.Backend.Messaging;
using Sdk.Messaging;

namespace DataCollectionWizard.Backend.Consumers;

public class GetDeviceTreeUpdateTokenRequestConsumer(IDeviceTreeUpdater updater) : RequestConsumer<GetDeviceTreeUpdateTokenRequest, GetDeviceTreeUpdateTokenResponse>
{
    public override Task<GetDeviceTreeUpdateTokenResponse> HandleException(GetDeviceTreeUpdateTokenRequest message, Exception e, CancellationToken cancellationToken)
        => Task.FromResult(new GetDeviceTreeUpdateTokenResponse
        {
            RequestError = new ErrorInfo(ErrorCodes.GetDeviceConnectorsFailed, e.Message),
        });

    public override async Task<GetDeviceTreeUpdateTokenResponse> Respond(GetDeviceTreeUpdateTokenRequest message, CancellationToken cancellationToken)
    {
        var token = await updater.RequestUpdateAsync(cancellationToken);

        return new GetDeviceTreeUpdateTokenResponse
        {
            Token = token,
            RequestError = null,
        };
    }
}
