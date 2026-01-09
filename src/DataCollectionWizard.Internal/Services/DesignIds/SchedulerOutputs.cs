namespace DataCollectionWizard.Internal.Services.DesignIds;

public sealed class SchedulerOutputs : IDesignIdStore
{
    public static SchedulerOutputs Instance { get; } = new SchedulerOutputs();

    public Guid Trigger { get; } = Guid.Parse("713c5651-a5a7-40df-8232-b3265dba32ec");
}
