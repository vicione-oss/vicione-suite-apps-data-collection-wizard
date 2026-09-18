using DataCollectionWizard.Internal.Contracts;
using Sdk.Connections.Contracts;
using ViciOne.DeviceTree.Contracts;

namespace DataCollectionWizard.Internal.Services.CloudDataflowGenerators;

public interface ICloudFilter
{
    Type CloudDataflowGeneratorType { get; }

    IReadOnlyCollection<Type> TreeNodesSupportedForConfiguration { get; }
    ConnectionKind ConnectionKind { get; }

    /// <summary>
    /// The compression intervals this cloud offers for a compressable data node, or empty if it cannot be
    /// configured at all.
    /// </summary>
    IReadOnlyCollection<AggregationInterval> SupportedAggregationIntervals { get; }

    /// <summary>
    /// The compression (aggregation) functions this cloud offers for a compressable data node, or empty if it
    /// cannot be configured at all.
    /// </summary>
    IReadOnlyCollection<AggregationFunction> SupportedAggregationFunctions { get; }

    IEnumerable<Connection> GetCloudConnections(IEnumerable<Connection> connections);
}
