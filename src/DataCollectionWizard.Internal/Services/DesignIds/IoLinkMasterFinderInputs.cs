namespace DataCollectionWizard.Internal.Services.DesignIds;

public sealed class IoLinkMasterFinderInputs : IDesignIdStore
{
    public static IoLinkMasterFinderInputs Instance { get; } = new IoLinkMasterFinderInputs();

    public Guid Trigger { get; } = Guid.Parse("8a66090b-a157-42f9-be3f-2b31f36e61df");
    public Guid TriggerNodeId { get; } = Guid.Parse("4f238ca3-482c-4be6-beb2-9281de30d6b8");
}
