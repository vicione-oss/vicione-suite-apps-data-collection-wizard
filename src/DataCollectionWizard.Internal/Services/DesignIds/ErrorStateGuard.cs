namespace DataCollectionWizard.Internal.Services.DesignIds;

public sealed class ErrorStateGuard : IFunctionBlock
{
    public Guid DesignId { get; } = Guid.Parse("5a60a3b1-c5b4-457a-8831-d535a383df4e");

    public ErrorStateGuardInputs Inputs { get; } = ErrorStateGuardInputs.Instance;
    public ErrorStateGuardOutputs Outputs { get; } = ErrorStateGuardOutputs.Instance;
    public ErrorStateGuardSettings Settings { get; } = ErrorStateGuardSettings.Instance;
}
