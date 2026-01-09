namespace DataCollectionWizard.Internal.Services.DesignIds;

public sealed class DataFormatterInputs : IDesignIdStore
{
    public static DataFormatterInputs Instance { get; } = new DataFormatterInputs();
    public Guid Enabled { get; } = Guid.Parse("ff3947ec-00ec-4de5-a530-694be80b1fd3");

    public Guid Value { get; } = Guid.Parse("a876ad5e-841d-4942-bc44-255f12581b5d");
}
