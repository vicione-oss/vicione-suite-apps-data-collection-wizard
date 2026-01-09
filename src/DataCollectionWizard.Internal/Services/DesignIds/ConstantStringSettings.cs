namespace DataCollectionWizard.Internal.Services.DesignIds;

public sealed class ConstantStringSettings : IDesignIdStore
{
    public static ConstantStringSettings Instance { get; } = new ConstantStringSettings();

    public Guid DefaultValue { get; } = Guid.Parse("cfbbdf29-a565-48cc-b119-3a91bb5f8541");
}
