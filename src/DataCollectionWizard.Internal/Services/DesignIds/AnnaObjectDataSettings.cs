namespace DataCollectionWizard.Internal.Services.DesignIds;

public sealed class AnnaObjectDataSettings : IDesignIdStore
{
    public static AnnaObjectDataSettings Instance { get; } = new();

    public Guid DatapointIdentifier { get; } = Guid.Parse("4415e39c-5092-4a9f-a30e-537fcbd67d9f");
    public Guid InsertAverage { get; } = Guid.Parse("3912e929-27b1-47b4-b707-4376e01113ee");
    public Guid InsertMaximum { get; } = Guid.Parse("7fc9672d-d26f-4b58-8c6b-a8988cf3fd74");
    public Guid InsertMinimum { get; } = Guid.Parse("4accfa96-60a2-44dc-9061-f53bc8542d25");
    public Guid InsertRefValue { get; } = Guid.Parse("e9e418c4-4a94-4316-b2d1-71c1ebfa0ddb");
    public Guid InsertRotSpeed { get; } = Guid.Parse("75e66de3-906b-4b43-afc1-e1fd93a5f161");
}
