namespace DataCollectionWizard.Internal.Services.DesignIds;

public sealed class AnnaRawDataSettings : IDesignIdStore
{
    public static AnnaRawDataSettings Instance { get; } = new AnnaRawDataSettings();

    public Guid Unit { get; } = Guid.Parse("b2f50440-40d4-42ab-9903-fb0ce5b20fa0");
}
