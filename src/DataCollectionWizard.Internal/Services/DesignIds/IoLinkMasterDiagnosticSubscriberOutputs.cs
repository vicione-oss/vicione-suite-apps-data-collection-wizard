namespace DataCollectionWizard.Internal.Services.DesignIds;

public sealed class IoLinkMasterDiagnosticSubscriberOutputs : IDesignIdStore
{
    public static IoLinkMasterDiagnosticSubscriberOutputs Instance { get; } = new IoLinkMasterDiagnosticSubscriberOutputs();

    public Guid Temperature { get; } = Guid.Parse("e4b3c64d-f566-457f-aca8-f7777256e938");
    public Guid TemperatureUnit { get; } = Guid.Parse("f1b192cf-f295-4ac2-b106-1d6b167d45d4");
    public Guid SupplyVoltage { get; } = Guid.Parse("ec36ccc3-1ef6-48db-8b1b-f51f18bd77f1");
    public Guid SupplyVoltageUnit { get; } = Guid.Parse("bcaa2df0-3850-414c-9ca9-480c440d51c2");
    public Guid Current { get; } = Guid.Parse("1d8253c4-5b02-4abb-b35f-b320b874b550");
    public Guid CurrentUnit { get; } = Guid.Parse("52f8121f-21da-4321-98c4-1d6e870ce5d5");
    public Guid SupervisionStatus { get; } = Guid.Parse("9d43f3ba-db60-4ebf-9272-76a4acf7de4b");
    public Guid Available { get; } = Guid.Parse("140200f7-0e85-4d74-95ef-f23a7e08cc81");
}
