namespace DataCollectionWizard.Internal.Services.DesignIds;

public sealed class BooleanToDoubleOutputs : IDesignIdStore
{
    public static BooleanToDoubleOutputs Instance { get; } = new BooleanToDoubleOutputs();

    public Guid Value { get; } = Guid.Parse("4c0a0a44-78d1-40ac-845f-14dc86821637");
}
