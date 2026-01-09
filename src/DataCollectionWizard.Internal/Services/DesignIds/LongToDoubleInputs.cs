namespace DataCollectionWizard.Internal.Services.DesignIds;

public sealed class LongToDoubleInputs : IDesignIdStore
{
    public static LongToDoubleInputs Instance { get; } = new LongToDoubleInputs();

    public Guid Value { get; } = Guid.Parse("90608468-d669-4380-ac1f-fab7c6af83a0");
}
