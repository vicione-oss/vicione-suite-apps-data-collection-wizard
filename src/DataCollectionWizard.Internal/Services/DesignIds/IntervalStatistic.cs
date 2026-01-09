namespace DataCollectionWizard.Internal.Services.DesignIds;

public sealed class IntervalStatistic : IFunctionBlock
{
    public Guid DesignId { get; } = Guid.Parse("b7177184-76db-4b58-93b8-bd238ac1e277");

    public IntervalStatisticInputs Inputs { get; } = IntervalStatisticInputs.Instance;
    public IntervalStatisticOutputs Outputs { get; } = IntervalStatisticOutputs.Instance;
    public IntervalStatisticSettings Settings { get; } = IntervalStatisticSettings.Instance;
}
