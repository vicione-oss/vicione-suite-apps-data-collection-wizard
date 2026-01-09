namespace DataCollectionWizard.Internal.Services.DesignIds;

public sealed class VseVariantSubscriber : IFunctionBlock
{
    public Guid DesignId { get; } = Guid.Parse("dcffafe9-51c4-4a42-82e8-7516edf7b837");

    public VseVariantSubscriberSettings Settings { get; } = VseVariantSubscriberSettings.Instance;
    public VseVariantSubscriberOutputs Outputs { get; } = VseVariantSubscriberOutputs.Instance;
}
