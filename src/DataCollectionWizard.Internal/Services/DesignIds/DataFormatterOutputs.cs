namespace DataCollectionWizard.Internal.Services.DesignIds;

public sealed class DataFormatterOutputs : IDesignIdStore
{
    public static DataFormatterOutputs Instance { get; } = new DataFormatterOutputs();

    public Guid FormattedValue { get; } = Guid.Parse("cbf0229b-eec6-47c4-97ab-e6c81e98bf42");
}
