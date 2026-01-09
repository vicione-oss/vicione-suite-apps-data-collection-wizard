namespace DataCollectionWizard.Internal.Services.DesignIds;

public sealed class DataFormatterSettings : IDesignIdStore
{
    public static DataFormatterSettings Instance { get; } = new DataFormatterSettings();

    public Guid ProcessId { get; } = Guid.Parse("56dd79be-a6d8-4b9c-8790-440c7903b00e");
    public Guid ThingId { get; } = Guid.Parse("0e77d852-841d-4c62-bb31-dc63d35ec0da");
}
