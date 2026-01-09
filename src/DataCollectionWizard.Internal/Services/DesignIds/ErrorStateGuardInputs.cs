namespace DataCollectionWizard.Internal.Services.DesignIds;

public sealed class ErrorStateGuardInputs : IDesignIdStore
{
    public static ErrorStateGuardInputs Instance { get; } = new ErrorStateGuardInputs();

    public Guid ErrorState { get; } = Guid.Parse("6953eb6b-1b00-4b42-886e-c0180e49cb72");
}
