using System.Globalization;
using ClusterManagement.Public.Iodds;
using DataCollectionWizard.Internal.Requests;
using Sdk.Backend.Messaging;

namespace DataCollectionWizard.Backend.Consumers;

public class GetIoddImageConsumer(IIoddStore ioddStore) : RequestConsumer<GetIoddImageRequest, GetIoddImageResponse>
{
    public override Task<GetIoddImageResponse> HandleException(GetIoddImageRequest message, Exception e, CancellationToken cancellationToken)
        => throw e;

    public override async Task<GetIoddImageResponse> Respond(GetIoddImageRequest message, CancellationToken cancellationToken)
    {
        var ioddImagePath = Path.Combine(
            ioddStore.IoddDirectory,
            message.VendorId.ToString(CultureInfo.InvariantCulture),
            message.DeviceId.ToString(CultureInfo.InvariantCulture),
            message.ImageFileName
        );

        if (!File.Exists(ioddImagePath))
        {
            return new GetIoddImageResponse()
            {
                ImageDataBase64 = string.Empty,
                RequestError = new(-10, "Argument exception: Target iodd image does not exist.")
            };
        }

        var imageData = await File.ReadAllBytesAsync(ioddImagePath, cancellationToken);
        var imageDataBase64 = Convert.ToBase64String(imageData);

        return new GetIoddImageResponse() { ImageDataBase64 = imageDataBase64, };
    }
}
