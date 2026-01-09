namespace DataCollectionWizard.Internal.Services.DesignIds;

public sealed class BlobSubscriber : IFunctionBlock
{
    public Guid DesignId { get; } = Guid.Parse("61b0e746-9834-483b-8bbe-7ed76654c6de");

    public BlobSubscriberInputs Inputs { get; } = BlobSubscriberInputs.Instance;
    public BlobSubscriberOutputs Outputs { get; } = BlobSubscriberOutputs.Instance;
    public BlobSubscriberSettings Settings { get; } = BlobSubscriberSettings.Instance;
}
