using System.Diagnostics;

namespace DataCollectionWizard.Internal.Contracts;

[DebuggerDisplay("{nameof(ProcessDataId),nq}={ProcessDataId} {nameof(ValueOutputId),nq}={ValueOutputId} {nameof(UnitOutputId),nq}={UnitOutputId}")]
public sealed class ValueMappingEntry
{
    public required string ProcessDataId { get; set; }
    public Guid? UnitOutputId { get; set; }
    public required Guid ValueOutputIdUI { get; set; }
    public required Guid? ValueOutputIdLogging { get; set; }
}
