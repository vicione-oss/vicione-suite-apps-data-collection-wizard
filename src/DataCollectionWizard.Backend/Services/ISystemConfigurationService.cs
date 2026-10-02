using Sdk.SystemConfiguration.Contracts;

namespace DataCollectionWizard.Backend.Services;

public interface ISystemConfigurationService
{
    Task<IReadOnlyList<NetworkInterface>> GetNetworkInterfacesAsync(CancellationToken cancellationToken);
}
