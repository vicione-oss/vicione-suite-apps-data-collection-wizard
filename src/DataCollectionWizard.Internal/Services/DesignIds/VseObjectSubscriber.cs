namespace DataCollectionWizard.Internal.Services.DesignIds;

public sealed class VseObjectSubscriber : IFunctionBlock
{
    public Guid DesignId { get; } = Guid.Parse("e20d1fa9-ec9e-46cc-b697-bd475090ec1a");

    public VseObjectSubscriberOutputs Outputs { get; } = VseObjectSubscriberOutputs.Instance;
    public VseObjectSubscriberSettings Settings { get; } = VseObjectSubscriberSettings.Instance;
}
