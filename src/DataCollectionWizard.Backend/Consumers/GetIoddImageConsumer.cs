using System.Globalization;
using ClusterManagement.Public.Iodds;
using DataCollectionWizard.Internal.Requests;
using MassTransit;
using Sdk.Messaging;

namespace DataCollectionWizard.Backend.Consumers;

public class GetIoddImageConsumer(IIoddStore ioddStore) : RequestConsumer<GetIoddImageRequest, GetIoddImageResponse>
{
    protected override Task<GetIoddImageResponse> HandleException(ConsumeContext<GetIoddImageRequest> context, Exception e)
        => throw new NotImplementedException();

    protected override async Task<GetIoddImageResponse> Respond(ConsumeContext<GetIoddImageRequest> context)
    {
        var ioddImagePath = Path.Combine(
            ioddStore.IoddDirectory,
            context.Message.VendorId.ToString(CultureInfo.InvariantCulture),
            context.Message.DeviceId.ToString(CultureInfo.InvariantCulture),
            context.Message.ImageFileName
        );

        if (!File.Exists(ioddImagePath))
        {
            return new GetIoddImageResponse()
            {
                ImageDataBase64 = string.Empty,
                RequestError = new(-10, "Argument exception: Target iodd image does not exist.")
            };
        }

        var imageData = await File.ReadAllBytesAsync(ioddImagePath);
        var imageDataBase64 = Convert.ToBase64String(imageData);

        return new GetIoddImageResponse() { ImageDataBase64 = imageDataBase64, };
    }
}
