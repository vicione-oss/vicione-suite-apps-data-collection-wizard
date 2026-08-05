using DataCollectionWizard.Internal.Services.CloudDataflowGenerators;

internal class TreeModel
{
    public List<TreeModel> Children { get; set; } = new();
    public required string Id { get; set; }
    public required string Name { get; set; }
    public ProcessDataConfiguration? DataConfig { get; set; }
}
