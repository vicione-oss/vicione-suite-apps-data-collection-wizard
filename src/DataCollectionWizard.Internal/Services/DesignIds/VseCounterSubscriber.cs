namespace DataCollectionWizard.Internal.Services.DesignIds;

public sealed class VseCounterSubscriber : IFunctionBlock
{
    public Guid DesignId { get; } = Guid.Parse("6312e770-3c86-45ee-90d5-1e417929dbbc");

    public VseCounterSubscriberOutputs Outputs { get; } = VseCounterSubscriberOutputs.Instance;
    public VseCounterSubscriberSettings Settings { get; } = VseCounterSubscriberSettings.Instance;
}
