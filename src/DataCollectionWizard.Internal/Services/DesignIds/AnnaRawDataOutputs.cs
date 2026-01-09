namespace DataCollectionWizard.Internal.Services.DesignIds;

public sealed class AnnaRawDataOutputs : IDesignIdStore
{
    public static AnnaRawDataOutputs Instance { get; } = new AnnaRawDataOutputs();

    public Guid Data { get; } = Guid.Parse("294357c4-b9d6-4745-b3ef-b6522e8975c8");
}
