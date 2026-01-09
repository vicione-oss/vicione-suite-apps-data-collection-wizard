namespace DataCollectionWizard.Internal.Services.DesignIds;

public sealed class ErrorStateGuardSettings : IDesignIdStore
{
    public static ErrorStateGuardSettings Instance { get; } = new ErrorStateGuardSettings();

    public Guid Delay { get; } = Guid.Parse("59ecc6ca-1b08-450d-afe6-bb72d5da2e73");
    public Guid OnDamage { get; } = Guid.Parse("57f2bcf3-cdb5-42c1-bb29-a1ae5329c964");
    public Guid OnWarning { get; } = Guid.Parse("85659b86-73a0-4371-9c3d-3ba24c23f049");
}
