namespace DataCollectionWizard.Internal.Services.DesignIds;

public sealed class IntervalStatisticOutputs : IDesignIdStore
{
    public static IntervalStatisticOutputs Instance { get; } = new IntervalStatisticOutputs();

    public Guid Average { get; } = Guid.Parse("700def8a-5ea1-47c2-8e8d-14fb3172a6d2");
    public Guid Difference { get; } = Guid.Parse("ffb7474d-df41-4bd1-9220-5aaf31aec334");
    public Guid Last { get; } = Guid.Parse("bf07d241-d366-48d2-b14d-0ebb4a289732");
    public Guid Maximum { get; } = Guid.Parse("8a3d9802-a9e6-40f7-a034-ee9e5872f806");
    public Guid Minimum { get; } = Guid.Parse("60c2ecd6-ea63-446d-8ab2-014476beb999");
    public Guid Sum { get; } = Guid.Parse("ab18d735-de39-4041-833a-47c3c5bd44fd");
}
