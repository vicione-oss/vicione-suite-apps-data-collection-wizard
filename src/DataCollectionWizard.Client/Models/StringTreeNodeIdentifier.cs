using ViciOne.Ui.TreeEditor.Builder.Interface.NodeIdentifier;

namespace DataCollectionWizard.Client.Models;

public class StringTreeNodeIdentifier : INodeIdentifier, IEquatable<string>
{
    public required string Value { get; set; }
    public string AsString => Value;

    public bool Equals(INodeIdentifier? other)
    {
        if (other is not StringTreeNodeIdentifier stringNodeIdentifier)
            return false;

        return string.Equals(Value, stringNodeIdentifier.Value, StringComparison.Ordinal);
    }

    public bool Equals(string? other)
        => Value.Equals(other, StringComparison.Ordinal);

    public override int GetHashCode()
        => Value.GetHashCode(StringComparison.Ordinal);
}
