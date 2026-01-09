namespace DataCollectionWizard.Internal.Services.DesignIds;

public sealed class LongToDoubleOutputs : IDesignIdStore
{
    public static LongToDoubleOutputs Instance { get; } = new LongToDoubleOutputs();

    public Guid Value { get; } = Guid.Parse("fafaca49-e805-4262-8d04-d190981f1cd3");
}
