namespace DataCollectionWizard.Internal.Services.DesignIds;

public sealed class LongToDouble : IFunctionBlock
{
    public Guid DesignId { get; } = Guid.Parse("08dc7173-c33f-49bf-83a4-3c001b450380");

    public LongToDoubleInputs Inputs { get; } = LongToDoubleInputs.Instance;
    public LongToDoubleOutputs Outputs { get; } = LongToDoubleOutputs.Instance;
}
