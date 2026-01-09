namespace DataCollectionWizard.Internal.Services.DesignIds;

public sealed class IoLinkDeviceTreeSubscriber : IFunctionBlock
{
    public Guid DesignId { get; } = Guid.Parse("d43d0507-5f31-4e56-aaed-4ca76a44d971");

    public IoLinkDeviceTreeSubscriberInputs Inputs { get; } = IoLinkDeviceTreeSubscriberInputs.Instance;
    public IoLinkDeviceTreeSubscriberOutputs Outputs { get; } = IoLinkDeviceTreeSubscriberOutputs.Instance;
    public IoLinkDeviceTreeSubscriberSettings Settings { get; } = IoLinkDeviceTreeSubscriberSettings.Instance;
}
