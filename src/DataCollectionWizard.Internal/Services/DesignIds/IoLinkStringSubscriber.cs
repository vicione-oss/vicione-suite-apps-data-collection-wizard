namespace DataCollectionWizard.Internal.Services.DesignIds;

public sealed class IoLinkStringSubscriber : IFunctionBlock
{
    public Guid DesignId { get; } = Guid.Parse("23213725-a157-4a4b-93c0-9bc0642e4944");

    public IoLinkStringSubscriberOutputs Outputs { get; } = IoLinkStringSubscriberOutputs.Instance;
    public IoLinkStringSubscriberSettings Settings { get; } = IoLinkStringSubscriberSettings.Instance;
}
