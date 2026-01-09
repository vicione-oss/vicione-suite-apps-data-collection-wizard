namespace DataCollectionWizard.Internal.Services.DesignIds;

public sealed class VseInputSubscriber : IFunctionBlock
{
    public Guid DesignId { get; } = Guid.Parse("bda228ee-19ca-4293-a94a-49ffab71c76d");

    public VseInputSubscriberOutputs Outputs { get; } = VseInputSubscriberOutputs.Instance;
    public VseInputSubscriberSettings Settings { get; } = VseInputSubscriberSettings.Instance;
}
