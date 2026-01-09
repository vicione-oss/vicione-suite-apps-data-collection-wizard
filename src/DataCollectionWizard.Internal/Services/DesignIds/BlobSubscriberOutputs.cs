namespace DataCollectionWizard.Internal.Services.DesignIds;

public sealed class BlobSubscriberOutputs : IDesignIdStore
{
    public static BlobSubscriberOutputs Instance { get; } = new BlobSubscriberOutputs();

    public Guid Data { get; } = Guid.Parse("a9abc094-d629-41ed-a0c7-0e3815734b45");
}
