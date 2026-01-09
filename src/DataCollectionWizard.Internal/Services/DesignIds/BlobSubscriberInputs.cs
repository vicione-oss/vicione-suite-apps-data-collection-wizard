namespace DataCollectionWizard.Internal.Services.DesignIds;

public sealed class BlobSubscriberInputs : IDesignIdStore
{
    public static BlobSubscriberInputs Instance { get; } = new BlobSubscriberInputs();

    public Guid Trigger { get; } = Guid.Parse("29265909-8da8-4cf0-acb8-812d6ebf025b");
}
