namespace DataCollectionWizard.Internal.Services.DesignIds;

public sealed class VseObjectSubscriberSettings : IDesignIdStore, IVseSubscriberSettings
{
    public static VseObjectSubscriberSettings Instance { get; } = new VseObjectSubscriberSettings();

    public Guid Address { get; } = Guid.Parse("225dac1a-a373-48c2-966f-b6cf38d62792");
    public Guid Path { get; } = Guid.Parse("614a1ffe-258d-48bf-ad91-d0e1b852d24d");
}
