namespace DataCollectionWizard.Internal.Services.DesignIds;

public sealed class IoTCoreConfiguration : IFunctionBlock
{
    public Guid DesignId { get; } = Guid.Parse("d43d0507-5f31-4e56-aaed-4ca76a44d971");

    public IoTCoreConfigurationInputs Inputs { get; } = IoTCoreConfigurationInputs.Instance;
    public IoTCoreConfigurationOutputs Outputs { get; } = IoTCoreConfigurationOutputs.Instance;
    public IoTCoreConfigurationSettings Settings { get; } = IoTCoreConfigurationSettings.Instance;
}
