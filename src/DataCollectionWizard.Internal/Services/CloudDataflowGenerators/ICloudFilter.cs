using Sdk.Connections.Contracts;

namespace DataCollectionWizard.Internal.Services.CloudDataflowGenerators;

public interface ICloudFilter
{
    Type CloudDataflowGeneratorType { get; }

    IReadOnlyCollection<Type> TreeNodesSupportedForConfiguration { get; }
    ConnectionKind ConnectionKind { get; }

    IEnumerable<Connection> GetCloudConnections(IEnumerable<Connection> connections);
}
