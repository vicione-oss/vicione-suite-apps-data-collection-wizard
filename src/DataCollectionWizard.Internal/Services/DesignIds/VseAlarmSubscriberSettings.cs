namespace DataCollectionWizard.Internal.Services.DesignIds;

public sealed class VseAlarmSubscriberSettings : IDesignIdStore, IVseSubscriberSettings
{
    public static VseAlarmSubscriberSettings Instance { get; } = new VseAlarmSubscriberSettings();

    public Guid Address { get; } = Guid.Parse("b1e6884a-0768-4f66-b503-8da34a645b6d");
    public Guid Path { get; } = Guid.Parse("080450c4-ca1f-4b27-8425-c4618ccbd7af");
}
