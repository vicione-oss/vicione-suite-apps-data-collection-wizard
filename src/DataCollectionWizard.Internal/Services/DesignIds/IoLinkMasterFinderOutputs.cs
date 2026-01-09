namespace DataCollectionWizard.Internal.Services.DesignIds;

public sealed class IoLinkMasterFinderOutputs : IDesignIdStore
{
    public static IoLinkMasterFinderOutputs Instance { get; } = new IoLinkMasterFinderOutputs();

    public Guid Devices { get; } = Guid.Parse("7f0170d9-b6d0-4554-b4b6-f5fa1902394e");
    public Guid DevicesNodeId { get; } = Guid.Parse("3027a975-0e81-487c-aa72-35828a63c68c");
}
