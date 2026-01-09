namespace DataCollectionWizard.Internal.Services.DesignIds;

public sealed class ConstantString : IFunctionBlock
{
    public Guid DesignId { get; } = Guid.Parse("e5490496-5e42-42f6-8930-3df23cbc384b");

    public ConstantStringOutputs Outputs { get; } = ConstantStringOutputs.Instance;
    public ConstantStringSettings Settings { get; } = ConstantStringSettings.Instance;
}
