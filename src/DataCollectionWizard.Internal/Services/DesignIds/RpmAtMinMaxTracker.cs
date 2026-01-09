namespace DataCollectionWizard.Internal.Services.DesignIds;

public sealed class RpmAtMinMaxTracker : IFunctionBlock
{
    public Guid DesignId { get; } = Guid.Parse("8b01c744-167d-4ba3-af3e-66050790ea57");

    public RpmAtMinMaxTrackerInputs Inputs { get; } = RpmAtMinMaxTrackerInputs.Instance;
    public RpmAtMinMaxTrackerOutputs Outputs { get; } = RpmAtMinMaxTrackerOutputs.Instance;
}
