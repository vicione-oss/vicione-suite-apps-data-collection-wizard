using ViciOne.Driver.IoTCore.Contracts.DeviceTree;

namespace DataCollectionWizard.Client.Extensions;

internal static class PoolingModeHelper
{
    private const string Cloud_Pooling_AVERAGE = "Average";
    private const string Cloud_Pooling_DIFF = "Difference";
    private const string Cloud_Pooling_LAST = "Last";
    private const string Cloud_Pooling_MAX = "Maximum";
    private const string Cloud_Pooling_MIN = "Minimum";
    private const string Cloud_Pooling_MIN_MAX_AVG = "Avg Min Max";
    private const string Cloud_Pooling_SUM = "Sum";

    public static string PoolingModeToString(this PoolingMode cloudPoolMode)
        => cloudPoolMode switch
        {
            PoolingMode.Avg => Cloud_Pooling_AVERAGE,
            PoolingMode.Diff => Cloud_Pooling_DIFF,
            PoolingMode.Last => Cloud_Pooling_LAST,
            PoolingMode.Min => Cloud_Pooling_MIN,
            PoolingMode.Max => Cloud_Pooling_MAX,
            PoolingMode.Sum => Cloud_Pooling_SUM,
            PoolingMode.MinMaxAvg => Cloud_Pooling_MIN_MAX_AVG,
            _ => throw new NotImplementedException("Unknown pooling mode")
        };

    public static PoolingMode ToPoolingMode(this string cloudPoolMode)
        => cloudPoolMode switch
        {
            Cloud_Pooling_AVERAGE => PoolingMode.Avg,
            Cloud_Pooling_DIFF => PoolingMode.Diff,
            Cloud_Pooling_LAST => PoolingMode.Last,
            Cloud_Pooling_MIN => PoolingMode.Min,
            Cloud_Pooling_MAX => PoolingMode.Max,
            Cloud_Pooling_SUM => PoolingMode.Sum,
            Cloud_Pooling_MIN_MAX_AVG => PoolingMode.MinMaxAvg,
            _ => throw new NotImplementedException("Unknown pooling mode")
        };
}
