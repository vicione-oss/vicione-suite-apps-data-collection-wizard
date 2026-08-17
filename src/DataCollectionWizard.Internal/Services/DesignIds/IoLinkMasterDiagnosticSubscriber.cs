namespace DataCollectionWizard.Internal.Services.DesignIds;

public sealed class IoLinkMasterDiagnosticSubscriber : IFunctionBlock
{
    public Guid DesignId { get; } = Guid.Parse("360b6eb4-d964-4e71-8757-634fa3a9dc36");

    public IoLinkMasterDiagnosticSubscriberOutputs Outputs { get; } = IoLinkMasterDiagnosticSubscriberOutputs.Instance;
    public IoLinkMasterDiagnosticSubscriberSettings Settings { get; } = IoLinkMasterDiagnosticSubscriberSettings.Instance;
}
