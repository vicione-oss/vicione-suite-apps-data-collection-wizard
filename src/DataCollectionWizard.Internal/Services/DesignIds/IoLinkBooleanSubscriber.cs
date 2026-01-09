namespace DataCollectionWizard.Internal.Services.DesignIds;

public sealed class IoLinkBooleanSubscriber : IFunctionBlock
{
    public Guid DesignId { get; } = Guid.Parse("1f855fea-ea87-4883-b310-a7db41cfaf50");

    public IoLinkBooleanSubscriberOutputs Outputs { get; } = IoLinkBooleanSubscriberOutputs.Instance;
    public IoLinkBooleanSubscriberSettings Settings { get; } = IoLinkBooleanSubscriberSettings.Instance;
}
