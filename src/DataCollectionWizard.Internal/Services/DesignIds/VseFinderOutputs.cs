namespace DataCollectionWizard.Internal.Services.DesignIds;

public sealed class VseFinderOutputs : IDesignIdStore
{
    public static VseFinderOutputs Instance { get; } = new VseFinderOutputs();

    public Guid Devices { get; } = Guid.Parse("26f1db36-c96c-4515-a2b1-06d989de063c");
    public Guid DevicesNodeId { get; } = Guid.Parse("0a72e10d-d833-4faa-b751-b9ca236446ea");
}
