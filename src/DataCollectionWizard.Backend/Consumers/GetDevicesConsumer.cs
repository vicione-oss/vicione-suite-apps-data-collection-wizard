using DataCollectionWizard.Backend.DbContext;
using DataCollectionWizard.Internal.Requests;
using DataCollectionWizard.Public;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Sdk.Backend.Messaging;
using Sdk.Messaging;

namespace DataCollectionWizard.Backend.Consumers;

public class GetDevicesConsumer(IDataCollectionWizardDbContext dbContext) : RequestConsumer<GetDevicesRequest, GetDevicesResponse>
{
    protected override Task<GetDevicesResponse> HandleException(ConsumeContext<GetDevicesRequest> context, Exception e)
        => Task.FromResult(new GetDevicesResponse
        {
            RequestError = new ErrorInfo(ErrorCodes.GetDevicesFailed, e.Message),
        });

    protected override async Task<GetDevicesResponse> Respond(ConsumeContext<GetDevicesRequest> context)
    {
        var dbItems = await dbContext.Devices.AsNoTracking().Select(k => k.DeviceAddress).ToListAsync();

        return new GetDevicesResponse
        {
            Devices = dbItems,
            RequestError = null,
        };
    }
}
