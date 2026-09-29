using Sdk.SystemConfiguration.Contracts;

namespace DataCollectionWizard.Internal.Services;

public interface ISystemConfigurationService
{
    Task<IReadOnlyList<NetworkInterface>> GetNetworkInterfacesAsync(CancellationToken cancellationToken);
}
