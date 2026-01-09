namespace DataCollectionWizard.Internal.Services.DesignIds;

public sealed class BooleanToDouble : IFunctionBlock
{
    public Guid DesignId { get; } = Guid.Parse("13fda31c-3e47-4637-815f-d2bb79196977");

    public BooleanToDoubleInputs Inputs { get; } = BooleanToDoubleInputs.Instance;
    public BooleanToDoubleOutputs Outputs { get; } = BooleanToDoubleOutputs.Instance;
}
