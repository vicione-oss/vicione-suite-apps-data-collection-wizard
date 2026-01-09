namespace DataCollectionWizard.Internal.Services.DesignIds;

public sealed class VseCounterSubscriberOutputs : IDesignIdStore
{
    public static VseCounterSubscriberOutputs Instance { get; } = new VseCounterSubscriberOutputs();

    public Guid Unit { get; } = Guid.Parse("b64dd41e-a568-4e29-b3b5-2169abaee054");
    public Guid Available { get; } = Guid.Parse("0f266bcf-c00b-4f5a-8406-e5fb586b3c91");
}
