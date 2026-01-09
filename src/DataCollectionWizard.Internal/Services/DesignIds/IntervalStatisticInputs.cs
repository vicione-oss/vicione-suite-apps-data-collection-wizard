namespace DataCollectionWizard.Internal.Services.DesignIds;

public sealed class IntervalStatisticInputs : IDesignIdStore
{
    public static IntervalStatisticInputs Instance { get; } = new IntervalStatisticInputs();

    public Guid Enabled { get; } = Guid.Parse("3226c055-4aae-4315-b096-ebdfac1248d3");
    public Guid Value { get; } = Guid.Parse("4f2dffe7-b12e-4798-a80b-57acd8093a38");
}
