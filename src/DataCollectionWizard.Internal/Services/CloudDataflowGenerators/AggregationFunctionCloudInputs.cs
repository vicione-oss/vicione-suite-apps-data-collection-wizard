
namespace DataCollectionWizard.Internal.Services.CloudDataflowGenerators;

public sealed class AggregationFunctionCloudInputs
{
    public CloudInput Avg { get; set; } = new();
    public CloudInput Last { get; set; } = new();
    public CloudInput Min { get; set; } = new();
    public CloudInput Max { get; set; } = new();
    public CloudInput RawData { get; set; } = new();
    public CloudInput RefValueAtMax { get; set; } = new();
    public CloudInput RefValueAtMin { get; set; } = new();
    public CloudInput RotSpeedAtMax { get; set; } = new();
    public CloudInput RotSpeedAtMin { get; set; } = new();
    public CloudInput RotationalFrequencies { get; set; } = new();
    public CloudInput Value { get; set; } = new();
}
