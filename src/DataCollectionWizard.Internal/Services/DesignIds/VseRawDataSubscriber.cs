namespace DataCollectionWizard.Internal.Services.DesignIds;

public sealed class VseRawDataSubscriber : IFunctionBlock
{
    public Guid DesignId { get; } = Guid.Parse("882057a8-2316-4bb4-8ca7-46840f44ddd6");

    public VseRawDataSubscriberInputs Inputs { get; } = VseRawDataSubscriberInputs.Instance;
    public VseRawDataSubscriberOutputs Outputs { get; } = VseRawDataSubscriberOutputs.Instance;
    public VseRawDataSubscriberSettings Settings { get; } = VseRawDataSubscriberSettings.Instance;
}
