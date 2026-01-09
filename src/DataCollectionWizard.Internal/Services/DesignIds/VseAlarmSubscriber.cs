namespace DataCollectionWizard.Internal.Services.DesignIds;

public sealed class VseAlarmSubscriber : IFunctionBlock
{
    public Guid DesignId { get; } = Guid.Parse("dbe6a560-f406-44f6-8551-d6d566cb54ed");

    public VseAlarmSubscriberOutputs Outputs { get; } = VseAlarmSubscriberOutputs.Instance;
    public VseAlarmSubscriberSettings Settings { get; } = VseAlarmSubscriberSettings.Instance;
}
