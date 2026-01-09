namespace DataCollectionWizard.Internal.Services.DesignIds;

public sealed class VseVariantSubscriberSettings : IDesignIdStore, IVseSubscriberSettings
{
    public static VseVariantSubscriberSettings Instance { get; } = new VseVariantSubscriberSettings();

    public Guid Address { get; } = Guid.Parse("53e06fe6-dd7a-4d50-968d-a14c678ce349");
    public Guid Path { get; } = Guid.Parse("fdf66cf7-1f5d-422e-a790-16a24bfe7e89");
}
