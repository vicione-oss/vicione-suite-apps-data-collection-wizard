namespace DataCollectionWizard.Internal.Services.DesignIds;

public sealed class VseRawDataSubscriberOutputs : IDesignIdStore
{
    public static VseRawDataSubscriberOutputs Instance { get; } = new VseRawDataSubscriberOutputs();

    public Guid Data { get; } = Guid.Parse("514c891d-78c4-4e13-9990-44499323069a");
}
