using System.Diagnostics;

namespace DataCollectionWizard.Internal.Contracts;

[DebuggerDisplay("{nameof(ProcessDataId),nq}={ProcessDataId} {nameof(ValueOutputIdUI),nq}={ValueOutputIdUI} {nameof(ValueOutputIdLogging),nq}={ValueOutputIdLogging} {nameof(UnitOutputId),nq}={UnitOutputId}")]
public sealed class ValueMappingEntry
{
    public required string ProcessDataId { get; set; }
    public Guid? UnitOutputId { get; set; }
    public required Guid ValueOutputIdUI { get; set; }
    public required Guid? ValueOutputIdLogging { get; set; }
}
