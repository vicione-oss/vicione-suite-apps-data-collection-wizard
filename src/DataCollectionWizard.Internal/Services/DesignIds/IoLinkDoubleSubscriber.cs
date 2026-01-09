namespace DataCollectionWizard.Internal.Services.DesignIds;

public sealed class IoLinkDoubleSubscriber : IFunctionBlock
{
    public Guid DesignId { get; } = Guid.Parse("70b13cb7-af9e-42c5-bcf1-6249576326c5");

    public IoLinkDoubleSubscriberOutputs Outputs { get; } = IoLinkDoubleSubscriberOutputs.Instance;
    public IoLinkDoubleSubscriberSettings Settings { get; } = IoLinkDoubleSubscriberSettings.Instance;
}
