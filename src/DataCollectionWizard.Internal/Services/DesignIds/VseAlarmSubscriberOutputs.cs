namespace DataCollectionWizard.Internal.Services.DesignIds;

public sealed class VseAlarmSubscriberOutputs : IDesignIdStore
{
    public static VseAlarmSubscriberOutputs Instance { get; } = new VseAlarmSubscriberOutputs();

    public Guid Value { get; } = Guid.Parse("bd69cffd-79af-438d-8c60-99430f44b63b");
    public Guid NumericValue { get; } = Guid.Parse("eca1448a-0f8c-40b3-a606-77d940b2a487");
    public Guid Available { get; } = Guid.Parse("90f17401-b5b9-4c92-93f7-e244c0478fdb");
}
