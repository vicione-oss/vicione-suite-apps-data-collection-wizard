namespace DataCollectionWizard.Internal.Services.CloudDataflowGenerators;

internal sealed class TreeModel
{
    public List<TreeModel> Children { get; set; } = [];
    public required string Id { get; set; }
    public required string Name { get; set; }
    public ProcessDataConfiguration? DataConfig { get; set; }
}
