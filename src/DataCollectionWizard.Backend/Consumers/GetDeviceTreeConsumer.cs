using DataCollectionWizard.Backend.Factories;
using DataCollectionWizard.Backend.Services;
using DataCollectionWizard.Public;
using DataCollectionWizard.Public.Requests;
using Microsoft.Extensions.Logging;
using Sdk.Backend.Messaging;
using Sdk.Messaging;
using ViciOne.Driver.IoTCore.Contracts.DeviceTree;

namespace DataCollectionWizard.Backend.Consumers;

public sealed class GetDeviceTreeConsumer(IDataCollectionWizardService dataCollectionWizard, ILogger<GetDeviceTreeConsumer> logger) : RequestConsumer<GetDeviceTree, GetDeviceTreeResponse>
{
    public override Task<GetDeviceTreeResponse> HandleException(GetDeviceTree message, Exception e, CancellationToken cancellationToken)
        => Task.FromResult(new GetDeviceTreeResponse
        {
            DeviceTree = new DeviceTreeRoot { IsOffline = true },
            RequestError = new ErrorInfo(ErrorCodes.GetDeviceTreeFailed, e.Message),
        });

    public override async Task<GetDeviceTreeResponse> Respond(GetDeviceTree message, CancellationToken cancellationToken)
    {
        // keep fake for test purposes
        if (message.FakeData)
        {
            logger.LogDebug("Respond with fake DeviceTree");

            var deviceTree = FakeDeviceTreeFactory.CreateDeviceTree();

            return new GetDeviceTreeResponse
            {
                DeviceTree = deviceTree,
                RequestError = null
            };
        }

        var tree = await dataCollectionWizard.RequestDeviceTreeAsync(cancellationToken);
        logger.LogDebug("Respond with DeviceTree from database with {Count} children", tree.Children.Count);

        return new GetDeviceTreeResponse
        {
            DeviceTree = tree,
            RequestError = null,
        };
    }
}
