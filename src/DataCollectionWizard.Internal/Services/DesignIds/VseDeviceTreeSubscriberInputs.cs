namespace DataCollectionWizard.Internal.Services.DesignIds;

public sealed class VseDeviceTreeSubscriberInputs : IDesignIdStore
{
    public static VseDeviceTreeSubscriberInputs Instance { get; } = new VseDeviceTreeSubscriberInputs();

    public Guid Trigger { get; } = Guid.Parse("c3f46bc9-2ce3-4169-a061-2af95dc50fc8");
}
