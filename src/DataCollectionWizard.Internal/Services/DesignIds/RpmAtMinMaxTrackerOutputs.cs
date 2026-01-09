namespace DataCollectionWizard.Internal.Services.DesignIds;

public sealed class RpmAtMinMaxTrackerOutputs : IDesignIdStore
{
    public static RpmAtMinMaxTrackerOutputs Instance { get; } = new RpmAtMinMaxTrackerOutputs();

    public Guid Average { get; } = Guid.Parse("1aa4398b-3a45-4433-9b79-cefd281ea3b8");
    public Guid Maximum { get; } = Guid.Parse("214febc9-7e9d-4157-846c-b668d423ecc4");
    public Guid Minimum { get; } = Guid.Parse("c263ff39-962a-4ff5-942a-ecb7160696b8");
    public Guid RefValueAtMaximum { get; } = Guid.Parse("cbdda594-4f8d-4da9-a664-9aa45001fadf");
    public Guid RefValueAtMinimum { get; } = Guid.Parse("8414706a-0b94-4efc-87af-4e10ed3c1299");
    public Guid RotSpeedAtMaximum { get; } = Guid.Parse("dfba1ff7-924b-47af-a753-63ebf973653a");
    public Guid RotSpeedAtMinimum { get; } = Guid.Parse("ff567407-9948-4c4f-a86a-d775421f660d");
}
