namespace DataCollectionWizard.Internal.Services.DesignIds;

public sealed class RpmAtMinMaxTrackerInputs : IDesignIdStore
{
    public static RpmAtMinMaxTrackerInputs Instance { get; } = new RpmAtMinMaxTrackerInputs();

    public Guid Average { get; } = Guid.Parse("9f0d4c0c-b45e-4294-b1b0-c6f8eed0f103");
    public Guid Difference { get; } = Guid.Parse("291d3f55-6970-4f13-9a1a-008adcc124d0");
    public Guid Maximum { get; } = Guid.Parse("ecdac43a-8ed4-48f6-8e99-91bb85dfcd3c");
    public Guid Minimum { get; } = Guid.Parse("864724a2-729f-4df0-83b5-18032ca9098b");
    public Guid RefValue { get; } = Guid.Parse("6d8de9e0-a7be-4553-a04b-496eb7fe8c4d");
    public Guid RotSpeed { get; } = Guid.Parse("0d46c06e-20be-4b70-82e4-c1771fa9569f");
    public Guid Sum { get; } = Guid.Parse("8d4a8f9b-a901-408b-a61d-16a68fa2a78b");
    public Guid Value { get; } = Guid.Parse("aaa824a8-3192-422e-9d8f-8b3c34558791");
}
