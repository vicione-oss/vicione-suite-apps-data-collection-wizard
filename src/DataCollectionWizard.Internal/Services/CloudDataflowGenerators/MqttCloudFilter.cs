using DataCollectionWizard.Internal.Contracts;
using DataCollectionWizard.Public;
using Sdk.Connections.Contracts;
using ViciOne.DeviceTree.Contracts;

namespace DataCollectionWizard.Internal.Services.CloudDataflowGenerators;

public sealed class MqttCloudFilter : ICloudFilter
{
    public Type CloudDataflowGeneratorType => typeof(MqttCloudDataflowGenerator);

    public IReadOnlyCollection<Type> TreeNodesSupportedForConfiguration => [typeof(IDeviceTreeCompressableDataNode),];

    public ConnectionKind ConnectionKind => ConnectionKind.Mqtt;

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
        => [.. connections.Where(IsMqttConnection)];

    public static bool IsMqttConnection(Connection connection)
        => connection.Type == ConnectionType.Mqtt && !connection.Managed && !connection.Tags.Contains(Constants.MoneoConnectCloud);
}
