using DataCollectionWizard.Backend.DbContext;
using DataCollectionWizard.Internal.Requests;
using DataCollectionWizard.Public;
using Microsoft.EntityFrameworkCore;
using Sdk.Backend.Messaging;
using Sdk.Messaging;

namespace DataCollectionWizard.Backend.Consumers;

// TODO: add tests
public class GetOutputConnectorMappingRequestConsumer(IDataCollectionWizardDbContext dbContext) : RequestConsumer<GetOutputConnectorMappingRequest, GetOutputConnectorMappingResponse>
{
    public override Task<GetOutputConnectorMappingResponse> HandleException(GetOutputConnectorMappingRequest message, Exception e, CancellationToken cancellationToken)
        => Task.FromResult(new GetOutputConnectorMappingResponse
        {
            Mapping = [],
            RequestError = new ErrorInfo(ErrorCodes.GetOutputConnectorMappingFailed, e.Message),
        });

    public override async Task<GetOutputConnectorMappingResponse> Respond(GetOutputConnectorMappingRequest message, CancellationToken cancellationToken)
    {
        var result = await dbContext.ValueMappings.AsNoTracking()
            .ToListAsync(cancellationToken);

        return new GetOutputConnectorMappingResponse
        {
            Mapping = result,
            RequestError = null,
        };
    }
}
