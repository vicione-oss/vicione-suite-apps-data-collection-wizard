namespace DataCollectionWizard.Internal.Services.DesignIds;

public sealed class AnnaObjectDataInputs : IDesignIdStore
{
    public static AnnaObjectDataInputs Instance { get; } = new();

    public Guid Average { get; } = Guid.Parse("7e89c3ba-074c-471f-adf4-a5bb466ce0fc");
    public Guid Maximum { get; } = Guid.Parse("bfc9f115-31a1-4dde-b918-bc61d3b0ff76");
    public Guid Minimum { get; } = Guid.Parse("9f313508-e4f9-40be-800d-8c87b2431103");
    public Guid RefValueAtMaximum { get; } = Guid.Parse("0345e9f9-d5f1-45ff-ba01-26106468ffb9");
    public Guid RefValueAtMinimum { get; } = Guid.Parse("2bf01d7f-aa4c-484e-b2f4-0eecf73be35b");
    public Guid RotSpeedAtMaximum { get; } = Guid.Parse("f2d19f16-ace6-4850-91f4-436d82d05673");
    public Guid RotSpeedAtMinimum { get; } = Guid.Parse("f8e7ed08-8f0c-402c-b9db-97fd3905255c");
    public Guid Value { get; } = Guid.Parse("93535d7e-633f-4e58-bb1d-ed0462fc43a5");
}
