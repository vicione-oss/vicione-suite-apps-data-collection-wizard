namespace DataCollectionWizard.Internal.Services.DesignIds;

public sealed class DataFormatter : IFunctionBlock
{
    public Guid DesignId { get; } = Guid.Parse("9cc8e4cc-fbdc-41a1-b83a-4af6d8f427b9");

    public DataFormatterInputs Inputs { get; } = DataFormatterInputs.Instance;
    public DataFormatterOutputs Outputs { get; } = DataFormatterOutputs.Instance;
    public DataFormatterSettings Settings { get; } = DataFormatterSettings.Instance;
}
