namespace DataCollectionWizard.Internal.Services.DesignIds;

public sealed class VseDeviceTreeSubscriber : IFunctionBlock
{
    public Guid DesignId { get; } = Guid.Parse("8a3246e7-c0a1-44df-8e1e-0acdad717432");

    public VseDeviceTreeSubscriberInputs Inputs { get; } = VseDeviceTreeSubscriberInputs.Instance;
    public VseDeviceTreeSubscriberOutputs Outputs { get; } = VseDeviceTreeSubscriberOutputs.Instance;
    public VseDeviceTreeSubscriberSettings Settings { get; } = VseDeviceTreeSubscriberSettings.Instance;
}
