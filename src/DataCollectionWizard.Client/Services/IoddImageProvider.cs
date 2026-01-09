using DataCollectionWizard.Internal.Requests;
using Sdk.Client.Infrastructure;

namespace DataCollectionWizard.Client.Services;

internal sealed class IoddImageProvider(IUiMediator mediator)
{
    public async Task<string> GetIoddImageDataBase64Async(ushort vendorId, uint deviceId, string imageFileName)
    {
        var response = await mediator.Request<GetIoddImageRequest, GetIoddImageResponse>(new(vendorId, deviceId, imageFileName));
        return response.ImageDataBase64;
    }
}
