namespace DataCollectionWizard.Internal.Services.DesignIds;

public sealed class VseVariantSubscriberOutputs : IDesignIdStore
{
    public static VseVariantSubscriberOutputs Instance { get; } = new VseVariantSubscriberOutputs();

    public Guid ActiveVariant { get; } = Guid.Parse("2f5b156d-39a1-4698-af59-278c8e80d1f5");
    public Guid Available { get; } = Guid.Parse("c9fb7647-7708-44b5-9ab4-d09176f7d4e9");
}
