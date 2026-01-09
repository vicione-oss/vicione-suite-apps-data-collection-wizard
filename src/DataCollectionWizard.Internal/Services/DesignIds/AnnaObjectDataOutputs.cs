namespace DataCollectionWizard.Internal.Services.DesignIds;

public sealed class AnnaObjectDataOutputs : IDesignIdStore
{
    public static AnnaObjectDataOutputs Instance { get; } = new();

    public Guid Value { get; } = Guid.Parse("6d3979bc-476c-47d5-96dd-62857562bf73");
}
