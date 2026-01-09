namespace DataCollectionWizard.Internal.Services.DesignIds;

public sealed class AnnaObjectData : IFunctionBlock
{
    public Guid DesignId { get; } = Guid.Parse("1850c411-8597-4abb-a155-c528afdaf2d1");

    public AnnaObjectDataInputs Inputs => AnnaObjectDataInputs.Instance;
    public AnnaObjectDataOutputs Outputs => AnnaObjectDataOutputs.Instance;
    public AnnaObjectDataSettings Settings => AnnaObjectDataSettings.Instance;
}
