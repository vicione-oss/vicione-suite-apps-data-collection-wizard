using DataCollectionWizard.Public;
using Sdk.Connections.Contracts;

namespace DataCollectionWizard.Internal.Services.CloudDataflowGenerators;

public sealed class AnnaCloudFilter : ICloudFilter
{
    public Type CloudDataflowGeneratorType => typeof(AnnaCloudDataflowGenerator);

    public IEnumerable<Connection> GetCloudConnections(IEnumerable<Connection> connections)
        => [.. connections.Where(IsAnnaConnection)];

    public static bool IsAnnaConnection(Connection connection)
        => connection.Tags.Contains(Constants.AnnaCloud) && connection.Type == ConnectionType.Http;
}
