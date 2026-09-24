using ClusterManagement.Public.Connections.Extensions;
using DataCollectionWizard.Internal.Contracts;
using Sdk.Connections.Contracts;
using ViciOne.DeviceTree.Contracts;

namespace DataCollectionWizard.Internal.Services.CloudDataflowGenerators;

public class OpcUaCloudFilter : ICloudFilter
{
    public Type CloudDataflowGeneratorType => typeof(OpcUaCloudDataflowGenerator);

    public IReadOnlyCollection<Type> TreeNodesSupportedForConfiguration => [typeof(IDeviceTreeCompressableDataNode),];

    public ConnectionKind ConnectionKind => ConnectionKind.OpcUa;

    public IReadOnlyCollection<AggregationInterval> SupportedAggregationIntervals => [
        AggregationInterval.OnChange,
        AggregationInterval.SecondsOne,
        AggregationInterval.SecondsFive,
        AggregationInterval.SecondsTen,
        AggregationInterval.SecondsThirty,
        AggregationInterval.MinutesOne,
        AggregationInterval.MinutesTwo,
        AggregationInterval.MinutesFive,
        AggregationInterval.MinutesTen,
        AggregationInterval.MinutesThirty,
        AggregationInterval.HoursOne,
        ];

    public IReadOnlyCollection<AggregationFunction> SupportedAggregationFunctions => [
        AggregationFunction.Avg,
        AggregationFunction.Min,
        AggregationFunction.Max,
        AggregationFunction.Last,
        ];

    public IEnumerable<Connection> GetCloudConnections(IEnumerable<Connection> connections)
        => [.. connections.Where(k => k.Type == ConnectionType.OpcUaServer)];
}
