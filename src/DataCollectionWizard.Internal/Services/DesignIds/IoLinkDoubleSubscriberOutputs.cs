namespace DataCollectionWizard.Internal.Services.DesignIds;

public sealed class IoLinkDoubleSubscriberOutputs : IDesignIdStore, IIoLinkSubscriberOutputs
{
    public static IoLinkDoubleSubscriberOutputs Instance { get; } = new IoLinkDoubleSubscriberOutputs();

    public Guid Unit { get; } = Guid.Parse("2a16682a-aabc-4fda-abb0-87f0f15882e0");
    public Guid Value { get; } = Guid.Parse("37acbdbc-decf-470d-963b-f4c36336aee9");
    public Guid Available { get; } = Guid.Parse("57a3f618-2d28-43a4-b0f4-b03aeab78e44");

    //Already numeric, does not have seperate numeric value output
    public Guid? NumericValue => Value;
}
