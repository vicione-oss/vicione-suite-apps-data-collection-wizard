namespace DataCollectionWizard.Internal.Services.DesignIds;

public sealed class IoLinkMasterDiagnosticSubscriberSettings : IDesignIdStore
{
    public static IoLinkMasterDiagnosticSubscriberSettings Instance { get; } = new IoLinkMasterDiagnosticSubscriberSettings();

    public Guid Identifier { get; } = Guid.Parse("94adb2d0-c229-47d6-99ef-30e1337c4313");
}
