namespace DataCollectionWizard.Internal.Services.DesignIds;

public sealed class IntervalStatisticSettings : IDesignIdStore
{
    public static IntervalStatisticSettings Instance { get; } = new IntervalStatisticSettings();

    public Guid CompressionTime { get; } = Guid.Parse("ac25b1ff-6683-401f-bb4d-6a7a0578742c");
}
