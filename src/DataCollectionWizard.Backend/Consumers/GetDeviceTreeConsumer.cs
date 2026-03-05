using DataCollectionWizard.Backend.Factories;
using DataCollectionWizard.Backend.Services;
using DataCollectionWizard.Public;
using DataCollectionWizard.Public.Requests;
using MassTransit;
using Microsoft.Extensions.Logging;
using Sdk.Backend.Messaging;
using Sdk.Messaging;
using ViciOne.Driver.IoTCore.Contracts.DeviceTree;

namespace DataCollectionWizard.Backend.Consumers;

public sealed class GetDeviceTreeConsumer(IDataCollectionWizardService dataCollectionWizard, ILogger<GetDeviceTreeConsumer> logger) : RequestConsumer<GetDeviceTree, GetDeviceTreeResponse>
{
    protected override Task<GetDeviceTreeResponse> HandleException(ConsumeContext<GetDeviceTree> context, Exception e)
        => Task.FromResult(new GetDeviceTreeResponse
        {
            DeviceTree = new DeviceTreeRoot { IsOffline = true },
            RequestError = new ErrorInfo(ErrorCodes.GetDeviceTreeFailed, e.Message),
        });

    protected override async Task<GetDeviceTreeResponse> Respond(ConsumeContext<GetDeviceTree> context)
    {
        // keep fake for test purposes
        if (context.Message.FakeData)
        {
            logger.LogDebug("Respond with fake DeviceTree");

            var deviceTree = FakeDeviceTreeFactory.CreateDeviceTree();

            return new GetDeviceTreeResponse
            {
                DeviceTree = deviceTree,
                RequestError = null
            };
        }

        var tree = await dataCollectionWizard.RequestDeviceTreeAsync(context.CancellationToken);
        logger.LogDebug("Respond with DeviceTree from database with {Count} children", tree.Children.Count);

        return new GetDeviceTreeResponse
        {
            DeviceTree = tree,
            RequestError = null,
        };
    }
}
