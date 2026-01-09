using DataCollectionWizard.Backend.DbContext;
using DataCollectionWizard.Internal.Requests;
using DataCollectionWizard.Public;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Sdk.Messaging;

namespace DataCollectionWizard.Backend.Consumers;

// TODO: add tests
public class GetOutputConnectorMappingRequestConsumer(IDataCollectionWizardDbContext dbContext) : RequestConsumer<GetOutputConnectorMappingRequest, GetOutputConnectorMappingResponse>
{
    protected override Task<GetOutputConnectorMappingResponse> HandleException(ConsumeContext<GetOutputConnectorMappingRequest> context, Exception e)
        => Task.FromResult(new GetOutputConnectorMappingResponse
        {
            Mapping = [],
            RequestError = new ErrorInfo(ErrorCodes.GetOutputConnectorMappingFailed, e.Message),
        });

    protected override async Task<GetOutputConnectorMappingResponse> Respond(ConsumeContext<GetOutputConnectorMappingRequest> context)
    {
        var result = await dbContext.ValueMappings.AsNoTracking()
            .ToListAsync();

        return new GetOutputConnectorMappingResponse
        {
            Mapping = result,
            RequestError = null,
        };
    }
}
