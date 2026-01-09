using DataCollectionWizard.Internal.Services.DesignIds;

namespace DataCollectionWizard.Internal.Services;

public class IoTSubscriberDesignTuple
{
    public Guid FunctionBlock { get; set; }
    public required IIoLinkSubscriberOutputs Outputs { get; set; }
    public required IIoLinkSubscriberSettings Settings { get; set; }
}
