using DataCollectionWizard.Internal.Services;
using Sdk.Backend.Messaging;
using Sdk.SystemConfiguration.Contracts;
using Sdk.SystemConfiguration.Requests;

namespace DataCollectionWizard.Backend.Services;

// Reads the host's configuration as provided by the host management.
public class SystemConfigurationService(ISuiteMediator mediator) : ISystemConfigurationService
{
    public async Task<IReadOnlyList<NetworkInterface>> GetNetworkInterfacesAsync(CancellationToken cancellationToken)
    {
        var response = await mediator.Request<GetSystemConfiguration, GetSystemConfigurationResponse>(new GetSystemConfiguration(), cancellationToken);

        if (response.RequestError is not null || response.Configuration is null)
            throw new InvalidOperationException($"Failed to get the system configuration from the host management: {response.RequestError?.Message ?? "no configuration returned"}");

        return response.Configuration.NetworkInterfaces;
    }
}
