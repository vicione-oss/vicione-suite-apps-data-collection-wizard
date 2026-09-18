namespace DataCollectionWizard.Internal.Services.DesignIds;

public sealed class VseScannerInputs : IDesignIdStore
{
    public static VseScannerInputs Instance { get; } = new VseScannerInputs();

    public Guid Trigger { get; } = Guid.Parse("7229c094-2c07-4bc5-8a84-ab23a488684d");
    public Guid TriggerNodeId { get; } = Guid.Parse("03e1a7a3-374c-425e-9e95-9d60cce557b5");
}
