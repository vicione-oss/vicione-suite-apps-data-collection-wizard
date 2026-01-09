namespace DataCollectionWizard.Internal.Services.DesignIds;

public sealed class IoLinkBooleanSubscriberOutputs : IDesignIdStore, IIoLinkSubscriberOutputs
{
    public static IoLinkBooleanSubscriberOutputs Instance { get; } = new IoLinkBooleanSubscriberOutputs();

    public Guid Unit { get; } = Guid.Parse("456cd982-76b4-406b-881e-4a2ca5587d4d");
    public Guid Value { get; } = Guid.Parse("5324fe8b-da6d-42dd-a3ad-fbce337d4ff8");
    public Guid? NumericValue => Guid.Parse("c40fd331-05ea-4a1e-a122-43cd92e8d2b5");
    public Guid Available => Guid.Parse("4a70addc-a543-40df-9cc8-dc3150c01cfd");
}
