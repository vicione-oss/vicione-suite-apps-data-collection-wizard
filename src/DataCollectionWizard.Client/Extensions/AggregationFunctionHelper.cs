using ViciOne.DeviceTree.Contracts;

namespace DataCollectionWizard.Client.Extensions;

internal static class AggregationFunctionHelper
{
    private const string Cloud_Aggregation_AVERAGE = "Average";
    private const string Cloud_Aggregation_DIFF = "Difference";
    private const string Cloud_Aggregation_LAST = "Last";
    private const string Cloud_Aggregation_MAX = "Maximum";
    private const string Cloud_Aggregation_MIN = "Minimum";
    private const string Cloud_Aggregation_MIN_MAX_AVG = "Avg Min Max";
    private const string Cloud_Aggregation_SUM = "Sum";

    public static string AggregationFunctionToString(this AggregationFunction aggregationFunction)
        => aggregationFunction switch
        {
            AggregationFunction.Avg => Cloud_Aggregation_AVERAGE,
            AggregationFunction.Diff => Cloud_Aggregation_DIFF,
            AggregationFunction.Last => Cloud_Aggregation_LAST,
            AggregationFunction.Min => Cloud_Aggregation_MIN,
            AggregationFunction.Max => Cloud_Aggregation_MAX,
            AggregationFunction.Sum => Cloud_Aggregation_SUM,
            AggregationFunction.MinMaxAvg => Cloud_Aggregation_MIN_MAX_AVG,
            _ => throw new NotImplementedException("Unknown aggregation function")
        };

    public static AggregationFunction ToAggregationFunction(this string aggregationFunction)
        => aggregationFunction switch
        {
            Cloud_Aggregation_AVERAGE => AggregationFunction.Avg,
            Cloud_Aggregation_DIFF => AggregationFunction.Diff,
            Cloud_Aggregation_LAST => AggregationFunction.Last,
            Cloud_Aggregation_MIN => AggregationFunction.Min,
            Cloud_Aggregation_MAX => AggregationFunction.Max,
            Cloud_Aggregation_SUM => AggregationFunction.Sum,
            Cloud_Aggregation_MIN_MAX_AVG => AggregationFunction.MinMaxAvg,
            _ => throw new NotImplementedException("Unknown aggregation function")
        };
}
