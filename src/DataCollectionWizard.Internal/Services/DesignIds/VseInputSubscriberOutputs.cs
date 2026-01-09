namespace DataCollectionWizard.Internal.Services.DesignIds;

public sealed class VseInputSubscriberOutputs : IDesignIdStore
{
    public static VseInputSubscriberOutputs Instance { get; } = new VseInputSubscriberOutputs();

    public Guid Unit { get; } = Guid.Parse("9ec34ae4-84b7-4c69-b30c-b25c83388886");
    public Guid Available { get; } = Guid.Parse("1aedcdab-1eb8-42f7-a350-a8e32254aea5");
}
