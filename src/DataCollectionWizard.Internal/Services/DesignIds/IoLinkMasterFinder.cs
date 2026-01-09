namespace DataCollectionWizard.Internal.Services.DesignIds;

public sealed class IoLinkMasterFinder : IFunctionBlock
{
    public Guid DesignId { get; } = Guid.Parse("87f58ff5-f65a-4572-8e44-b9afe662ef72");

    public IoLinkMasterFinderInputs Inputs { get; } = IoLinkMasterFinderInputs.Instance;
    public IoLinkMasterFinderOutputs Outputs { get; } = IoLinkMasterFinderOutputs.Instance;
}
