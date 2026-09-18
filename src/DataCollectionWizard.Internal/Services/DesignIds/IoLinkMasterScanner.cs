namespace DataCollectionWizard.Internal.Services.DesignIds;

public sealed class IoLinkMasterScanner : IFunctionBlock
{
    public Guid DesignId { get; } = Guid.Parse("87f58ff5-f65a-4572-8e44-b9afe662ef72");

    public IoLinkMasterScannerInputs Inputs { get; } = IoLinkMasterScannerInputs.Instance;
    public IoLinkMasterScannerOutputs Outputs { get; } = IoLinkMasterScannerOutputs.Instance;
}
