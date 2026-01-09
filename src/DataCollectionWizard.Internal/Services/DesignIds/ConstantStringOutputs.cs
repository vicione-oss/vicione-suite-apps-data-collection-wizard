namespace DataCollectionWizard.Internal.Services.DesignIds;

public sealed class ConstantStringOutputs : IDesignIdStore
{
    public static ConstantStringOutputs Instance { get; } = new ConstantStringOutputs();

    public Guid Value { get; } = Guid.Parse("be435c3f-1d04-4857-acd4-a3ac8b9c27be");
}
