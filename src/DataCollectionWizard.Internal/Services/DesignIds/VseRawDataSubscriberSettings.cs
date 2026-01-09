namespace DataCollectionWizard.Internal.Services.DesignIds;

public sealed class VseRawDataSubscriberSettings : IDesignIdStore
{
    public static VseRawDataSubscriberSettings Instance { get; } = new VseRawDataSubscriberSettings();

    public Guid Address { get; } = Guid.Parse("344d17fd-7d62-4341-b05c-7621a4af0dd1");
    public Guid Duration { get; } = Guid.Parse("1fbb9658-e539-40b0-b584-ba9ac51e242a");
    public Guid IgnoreTimedValueArray { get; } = Guid.Parse("4cabd0ec-44a5-427a-838f-541703b516bd");
    public Guid SamplingRate { get; } = Guid.Parse("1b914f44-1dc6-4219-8b5c-e838befb9995");
    public Guid SensorIndex { get; } = Guid.Parse("b0f3cc24-31ef-4b40-a9ce-5f04fff1cbcf");
}
