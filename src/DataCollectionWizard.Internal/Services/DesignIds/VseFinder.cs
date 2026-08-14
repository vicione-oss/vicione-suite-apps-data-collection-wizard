namespace DataCollectionWizard.Internal.Services.DesignIds;

public sealed class VseFinder : IFunctionBlock
{
    public Guid DesignId { get; } = Guid.Parse("e909ab02-03d4-43a8-8af1-dfebf836c5d7");

    public VseFinderInputs Inputs { get; } = VseFinderInputs.Instance;
    public VseFinderOutputs Outputs { get; } = VseFinderOutputs.Instance;
}
