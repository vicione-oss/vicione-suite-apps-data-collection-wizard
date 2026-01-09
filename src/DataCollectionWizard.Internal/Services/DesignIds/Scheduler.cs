namespace DataCollectionWizard.Internal.Services.DesignIds;

public sealed class Scheduler : IFunctionBlock
{
    public Guid DesignId { get; } = Guid.Parse("9f7e3f96-93ff-470c-b550-20492a749659");

    public SchedulerOutputs Outputs { get; } = SchedulerOutputs.Instance;
    public SchedulerSettings Settings { get; } = SchedulerSettings.Instance;
}
