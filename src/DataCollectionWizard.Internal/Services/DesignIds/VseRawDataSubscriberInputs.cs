namespace DataCollectionWizard.Internal.Services.DesignIds;

public sealed class VseRawDataSubscriberInputs : IDesignIdStore
{
    public static VseRawDataSubscriberInputs Instance { get; } = new VseRawDataSubscriberInputs();

    public Guid ObjectTrigger { get; } = Guid.Parse("e17f1bc4-7c3a-4622-994b-4f3afc6a38c3");
    public Guid Trigger { get; } = Guid.Parse("e9e1860b-b551-4796-a35b-6218cfc889d2");
}
