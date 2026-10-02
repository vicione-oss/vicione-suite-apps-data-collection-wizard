using DataCollectionWizard.Internal.Contracts;
using DataCollectionWizard.Public;
using Sdk.Connections.Contracts;
using ViciOne.DeviceTree.Contracts;

namespace DataCollectionWizard.Internal.Services.CloudDataflowGenerators;

public sealed class MoneoCloudFilter : ICloudFilter
{
    public Type CloudDataflowGeneratorType => typeof(MoneoCloudDataflowGenerator);

    public IReadOnlyCollection<Type> TreeNodesSupportedForConfiguration { get; } = [];

    public ConnectionKind ConnectionKind => ConnectionKind.Moneo;

    public IReadOnlyCollection<AggregationInterval> SupportedAggregationIntervals { get; } = [
        AggregationInterval.SecondsOne,
        AggregationInterval.SecondsTen,
        AggregationInterval.MinutesOne,
        ];

    public IReadOnlyCollection<AggregationFunction> SupportedAggregationFunctions { get; } = [
        AggregationFunction.Avg,
        AggregationFunction.Min,
        AggregationFunction.Max,
        AggregationFunction.Last,
        ];

    public IEnumerable<Connection> GetCloudConnections(IEnumerable<Connection> connections)
        => [.. connections.Where(IsMoneoConnection)];

    public static bool IsMoneoConnection(Connection connection)
        => connection.Tags.Contains(Constants.MoneoConnectCloud) && connection.Type == ConnectionType.Mqtt;
}
