namespace DataCollectionWizard.Internal.Services.DesignIds;

public sealed class VseCounterSubscriberSettings : IDesignIdStore, IVseSubscriberSettings
{
    public static VseCounterSubscriberSettings Instance { get; } = new VseCounterSubscriberSettings();

    public Guid Address { get; } = Guid.Parse("1cdf45d6-808a-4862-850e-e451da25a7ed");
    public Guid Path { get; } = Guid.Parse("eb8fc77f-af96-45ba-968e-b45ed585477c");
}
