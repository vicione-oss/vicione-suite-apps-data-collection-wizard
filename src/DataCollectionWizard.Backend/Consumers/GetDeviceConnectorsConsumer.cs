using DataCollectionWizard.Backend.DbContext;
using DataCollectionWizard.Internal.Contracts;
using DataCollectionWizard.Internal.Requests;
using DataCollectionWizard.Public;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Sdk.Backend.Messaging;
using Sdk.Messaging;

namespace DataCollectionWizard.Backend.Consumers;

public class GetDeviceConnectorsConsumer(IDataCollectionWizardDbContext dbContext) : RequestConsumer<GetDeviceConnectorsRequest, GetDeviceConnectorsResponse>
{
    protected override Task<GetDeviceConnectorsResponse> HandleException(ConsumeContext<GetDeviceConnectorsRequest> context, Exception e)
        => Task.FromResult(new GetDeviceConnectorsResponse
        {
            Ids = [],
            RequestError = new ErrorInfo(ErrorCodes.GetDeviceConnectorsFailed, e.Message),
        });

    protected override async Task<GetDeviceConnectorsResponse> Respond(ConsumeContext<GetDeviceConnectorsRequest> context)
    {
        List<DeviceConnectorIds> result;

        if (context.Message.DeviceAddress is not null)
        {
            // get items related to one vse
            result = [.. dbContext.DeviceConnectorIds.AsNoTracking().Where(x => Equals(x.DeviceAddress, context.Message.DeviceAddress.ToString()))];
        }
        else
        {
            // get all items
            result = await dbContext.DeviceConnectorIds.AsNoTracking()
                .ToListAsync();
        }

        return new GetDeviceConnectorsResponse
        {
            Ids = result,
            RequestError = null,
        };
    }
}
