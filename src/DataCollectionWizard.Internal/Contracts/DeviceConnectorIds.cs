namespace DataCollectionWizard.Internal.Contracts;

public class DeviceConnectorIds : IEquatable<DeviceConnectorIds>
{
    public required string DeviceAddress { get; init; }
    public Guid DeviceTreeOutput { get; set; }
    public Guid TriggerInput { get; set; }

    public bool Equals(DeviceConnectorIds? other) =>
        DeviceAddress == other?.DeviceAddress &&
        DeviceTreeOutput == other.DeviceTreeOutput &&
        TriggerInput == other.TriggerInput;

    public override bool Equals(object? obj) => Equals(obj as DeviceConnectorIds);

    public override int GetHashCode() => HashCode.Combine(DeviceAddress, DeviceTreeOutput, TriggerInput);
}
