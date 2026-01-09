namespace DataCollectionWizard.Internal.Services.DesignIds;

public sealed class SchedulerSettings : IDesignIdStore
{
    public static SchedulerSettings Instance { get; } = new SchedulerSettings();

    public Guid Times { get; } = Guid.Parse("ffc710a1-e01d-4202-9d1c-7445c5505aa6");
}
