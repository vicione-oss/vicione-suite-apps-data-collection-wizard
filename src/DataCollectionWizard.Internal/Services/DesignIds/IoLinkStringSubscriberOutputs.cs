namespace DataCollectionWizard.Internal.Services.DesignIds;

public sealed class IoLinkStringSubscriberOutputs : IDesignIdStore, IIoLinkSubscriberOutputs
{
    public static IoLinkStringSubscriberOutputs Instance { get; } = new IoLinkStringSubscriberOutputs();

    public Guid Unit { get; } = Guid.Parse("1b3df6f5-10df-4fed-b018-7d1b503b7e7d");
    public Guid Value { get; } = Guid.Parse("928ef1a8-fa95-47e1-8815-4a200ddd49df");
    public Guid Available { get; } = Guid.Parse("4a0a04fb-8c2e-4aed-ac13-0beb767e4f1f");

    //not supported for logging
    public Guid? NumericValue => null;
}
