namespace DataCollectionWizard.Internal.Services.DesignIds;

public sealed class AnnaRawData : IFunctionBlock
{
    public Guid DesignId { get; } = Guid.Parse("29ee65dc-5d02-4414-b289-bd1a331fc721");

    public AnnaRawDataInputs Inputs { get; } = AnnaRawDataInputs.Instance;
    public AnnaRawDataOutputs Outputs { get; } = AnnaRawDataOutputs.Instance;
    public AnnaRawDataSettings Settings { get; } = AnnaRawDataSettings.Instance;
}
