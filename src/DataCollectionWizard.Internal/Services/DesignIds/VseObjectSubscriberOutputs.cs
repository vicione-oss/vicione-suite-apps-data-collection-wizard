namespace DataCollectionWizard.Internal.Services.DesignIds;

public sealed class VseObjectSubscriberOutputs : IDesignIdStore
{
    public static VseObjectSubscriberOutputs Instance { get; } = new VseObjectSubscriberOutputs();

    public Guid ErrorState { get; } = Guid.Parse("32c15026-9b38-4064-a209-af0ad017cc5a");
    public Guid RefValue { get; } = Guid.Parse("c771f93f-e7e9-4551-a6aa-bd1f76488769");
    public Guid RotationalFrequencyTuple { get; } = Guid.Parse("5529fee0-3437-4adc-83b6-9332f4094096");
    public Guid RotSpeed { get; } = Guid.Parse("436f1b47-ff5d-4b9d-bfee-64d37ffeaba6");
    public Guid Unit { get; } = Guid.Parse("de5a61ef-cadd-49a3-9203-b4293c5eb219");
    public Guid Valid { get; } = Guid.Parse("bb18a71b-f1a1-48ca-8e8a-e192919b0b8a");
    public Guid Available { get; } = Guid.Parse("c38f7f40-ef59-453e-80d6-59755ad24880");
}
