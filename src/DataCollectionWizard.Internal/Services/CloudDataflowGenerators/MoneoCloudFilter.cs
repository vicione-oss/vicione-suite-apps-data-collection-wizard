using DataCollectionWizard.Internal.Contracts;
using DataCollectionWizard.Public;
using Sdk.Connections.Contracts;
using ViciOne.DeviceTree.Contracts;

namespace DataCollectionWizard.Internal.Services.CloudDataflowGenerators;

public sealed class MoneoCloudFilter : ICloudFilter
{
    public Type CloudDataflowGeneratorType => typeof(MoneoCloudDataflowGenerator);

    public IReadOnlyCollection<Type> TreeNodesSupportedForConfiguration => [];

    public ConnectionKind ConnectionKind => ConnectionKind.Moneo;

    public IReadOnlyCollection<AggregationInterval> SupportedAggregationIntervals => [
        AggregationInterval.SecondsOne,
        AggregationInterval.SecondsTen,
        AggregationInterval.MinutesOne,
        ];

    public IReadOnlyCollection<AggregationFunction> SupportedAggregationFunctions => [
        AggregationFunction.Last,
        AggregationFunction.Avg,
        AggregationFunction.Min,
        AggregationFunction.Max,
        ];

    public IEnumerable<Connection> GetCloudConnections(IEnumerable<Connection> connections)
        => [.. connections.Where(k => k.Tags.Contains(Constants.MoneoConnectCloud) && k.Type == ConnectionType.Mqtt)];
}
