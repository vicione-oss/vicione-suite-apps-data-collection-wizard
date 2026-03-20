using DataCollectionWizard.Backend.DbContext;
using DataCollectionWizard.Internal.Requests;
using DataCollectionWizard.Public;
using Microsoft.EntityFrameworkCore;
using Sdk.Backend.Messaging;
using Sdk.Messaging;

namespace DataCollectionWizard.Backend.Consumers;

public class GetDevicesConsumer(IDataCollectionWizardDbContext dbContext) : RequestConsumer<GetDevicesRequest, GetDevicesResponse>
{
    public override Task<GetDevicesResponse> HandleException(GetDevicesRequest message, Exception e, CancellationToken cancellationToken)
        => Task.FromResult(new GetDevicesResponse
        {
            RequestError = new ErrorInfo(ErrorCodes.GetDevicesFailed, e.Message),
        });

    public override async Task<GetDevicesResponse> Respond(GetDevicesRequest message, CancellationToken cancellationToken)
    {
        var dbItems = await dbContext.Devices.AsNoTracking().Select(k => k.DeviceAddress).ToListAsync(cancellationToken);

        return new GetDevicesResponse
        {
            Devices = dbItems,
            RequestError = null,
        };
    }
}
