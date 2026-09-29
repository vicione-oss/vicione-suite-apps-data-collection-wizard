using ClusterManagement.Public.Connections.Extensions;
using DataCollectionWizard.Internal.Contracts;
using Sdk.Connections.Contracts;
using ViciOne.DeviceTree.Contracts;

namespace DataCollectionWizard.Internal.Services.CloudDataflowGenerators;

public sealed class OpcUaCloudFilter : ICloudFilter
{
    public Type CloudDataflowGeneratorType => typeof(OpcUaCloudDataflowGenerator);

    public IReadOnlyCollection<Type> TreeNodesSupportedForConfiguration { get; } = [typeof(IDeviceTreeCompressableDataNode),];

    public ConnectionKind ConnectionKind => ConnectionKind.OpcUa;

    public IReadOnlyCollection<AggregationInterval> SupportedAggregationIntervals { get; } = [
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

    public IReadOnlyCollection<AggregationFunction> SupportedAggregationFunctions { get; } = [
        AggregationFunction.Avg,
        AggregationFunction.Min,
        AggregationFunction.Max,
        AggregationFunction.Last,
        ];

    public IEnumerable<Connection> GetCloudConnections(IEnumerable<Connection> connections)
        => [.. connections.Where(IsOpcUaConnection)];

    public static bool IsOpcUaConnection(Connection connection)
        => connection.Type == ConnectionType.OpcUaServer;
}
