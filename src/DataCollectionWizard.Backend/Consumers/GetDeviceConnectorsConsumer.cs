using DataCollectionWizard.Backend.DbContext;
using DataCollectionWizard.Internal.Contracts;
using DataCollectionWizard.Internal.Requests;
using DataCollectionWizard.Public;
using Microsoft.EntityFrameworkCore;
using Sdk.Backend.Messaging;
using Sdk.Messaging;

namespace DataCollectionWizard.Backend.Consumers;

public class GetDeviceConnectorsConsumer(IDataCollectionWizardDbContext dbContext) : RequestConsumer<GetDeviceConnectorsRequest, GetDeviceConnectorsResponse>
{
    public override Task<GetDeviceConnectorsResponse> HandleException(GetDeviceConnectorsRequest message, Exception e, CancellationToken cancellationToken)
        => Task.FromResult(new GetDeviceConnectorsResponse
        {
            Ids = [],
            RequestError = new ErrorInfo(ErrorCodes.GetDeviceConnectorsFailed, e.Message),
        });

    public override async Task<GetDeviceConnectorsResponse> Respond(GetDeviceConnectorsRequest message, CancellationToken cancellationToken)
    {
        List<DeviceConnectorIds> result;

        if (message.DeviceAddress is not null)
        {
            // get items related to one vse
            result = await dbContext.DeviceConnectorIds
                .Where(x => Equals(x.DeviceAddress, message.DeviceAddress.ToString()))
                .AsNoTracking()
                .ToListAsync(cancellationToken);
        }
        else
        {
            // get all items
            result = await dbContext.DeviceConnectorIds
                .AsNoTracking()
                .ToListAsync(cancellationToken);
        }

        return new GetDeviceConnectorsResponse
        {
            Ids = result,
            RequestError = null,
        };
    }
}
