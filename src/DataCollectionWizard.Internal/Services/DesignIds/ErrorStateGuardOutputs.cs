namespace DataCollectionWizard.Internal.Services.DesignIds;

public sealed class ErrorStateGuardOutputs : IDesignIdStore
{
    public static ErrorStateGuardOutputs Instance { get; } = new ErrorStateGuardOutputs();

    public Guid ErrorState { get; } = Guid.Parse("c331f5e7-b689-4dcb-bdbf-2b7c2c484e16");
}
