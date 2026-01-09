using DataCollectionWizard.Public;
using Sdk.Connections.Contracts;

namespace DataCollectionWizard.Internal.Services.CloudDataflowGenerators;

public sealed class MoneoCloudFilter : ICloudFilter
{
    public Type CloudDataflowGeneratorType => typeof(MoneoCloudDataflowGenerator);

    public IEnumerable<Connection> GetCloudConnections(IEnumerable<Connection> connections)
        => [.. connections.Where(k => k.Tags.Contains(Constants.MoneoConnectCloud) && k.Type == ConnectionType.Mqtt)];
}
