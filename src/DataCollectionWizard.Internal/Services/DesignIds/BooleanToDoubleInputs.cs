namespace DataCollectionWizard.Internal.Services.DesignIds;

public sealed class BooleanToDoubleInputs : IDesignIdStore
{
    public static BooleanToDoubleInputs Instance { get; } = new BooleanToDoubleInputs();

    public Guid Value { get; } = Guid.Parse("7bbfff36-e8bd-4020-b9a0-a87c2a721144");
}
