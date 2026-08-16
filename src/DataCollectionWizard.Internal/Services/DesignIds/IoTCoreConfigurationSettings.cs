namespace DataCollectionWizard.Internal.Services.DesignIds;

public sealed class IoTCoreConfigurationSettings : IDesignIdStore
{
    public static IoTCoreConfigurationSettings Instance { get; } = new IoTCoreConfigurationSettings();

    public Guid Identifier { get; } = Guid.Parse("42ae537b-ec07-4b6d-9bf7-e2aa10e84f73");
    public Guid Address { get; } = Guid.Parse("588d363b-0823-47c9-adf7-37db039ac408");
    public Guid Username { get; } = Guid.Parse("95edbd0b-68b9-4837-b843-5d3ef47e1391");
    public Guid Password { get; } = Guid.Parse("ed4a3e98-09dd-4037-bf95-2765264c943f");
    public Guid IoddDirectory { get; } = Guid.Parse("dc73e1e1-1588-4269-88a4-1c06ec943c30");
    public Guid IoddAutoDownload { get; } = Guid.Parse("b556fca2-9ede-4a4b-a853-e58cb31acfcf");
    public Guid UseGetDataMulti { get; } = Guid.Parse("0156985f-7efe-44e0-900f-88e2c008b891");
}
