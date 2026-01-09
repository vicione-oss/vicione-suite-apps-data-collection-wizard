namespace DataCollectionWizard.Internal.Services.DesignIds;

public sealed class VseInputSubscriberSettings : IDesignIdStore, IVseSubscriberSettings
{
    public static VseInputSubscriberSettings Instance { get; } = new VseInputSubscriberSettings();

    public Guid Address { get; } = Guid.Parse("66655db9-fab0-4f7a-8e29-aacbe93d1f7a");
    public Guid Path { get; } = Guid.Parse("8474a1da-5b33-492c-bba7-7946b83ccdb8");
}
