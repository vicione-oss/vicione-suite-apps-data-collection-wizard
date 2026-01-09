namespace DataCollectionWizard.Internal.Services.DesignIds;

public sealed class AnnaRawDataInputs : IDesignIdStore
{
    public static AnnaRawDataInputs Instance { get; } = new AnnaRawDataInputs();

    public Guid Data { get; } = Guid.Parse("26946363-7167-40d1-b755-d40f946621f4");
    public Guid RotationalFrequencies { get; } = Guid.Parse("f946092b-d399-460b-8c44-7d6eb53c0912");
}
