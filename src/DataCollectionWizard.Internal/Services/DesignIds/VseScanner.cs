namespace DataCollectionWizard.Internal.Services.DesignIds;

public sealed class VseScanner : IFunctionBlock
{
    public Guid DesignId { get; } = Guid.Parse("e909ab02-03d4-43a8-8af1-dfebf836c5d7");

    public VseScannerInputs Inputs { get; } = VseScannerInputs.Instance;
    public VseScannerOutputs Outputs { get; } = VseScannerOutputs.Instance;
}
