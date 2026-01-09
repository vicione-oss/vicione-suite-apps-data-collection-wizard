namespace DataCollectionWizard.Internal.Services.DesignIds;

public interface IIoLinkSubscriberOutputs
{
    Guid Available { get; }
    Guid Unit { get; }
    Guid Value { get; }

    // not realy an output on all subscribers, perhaps we need a second interface?
    Guid? NumericValue { get; }
}
