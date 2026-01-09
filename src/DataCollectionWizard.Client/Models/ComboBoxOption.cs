namespace DataCollectionWizard.Client.Models;

internal sealed class ComboBoxOption<TValue>
{
    public required string Text { get; set; }
    public required TValue Value { get; set; }
}
