using BIMCapabilities.Contracts.Evidence;

namespace BIMCapabilities.Runtime.Evidence;

/// <summary>
/// In-memory evidence collection for runtime composition.
/// </summary>
public sealed class RuntimeEvidenceSkeleton : IRuntimeEvidence
{
    private readonly List<EvidenceRecord> _records = [];

    public EvidenceCollection Collection =>
        new()
        {
            CollectionId = "runtime-evidence",
            Records = _records
        };

    public void Add(EvidenceRecord record)
    {
        ArgumentGuard.ThrowIfNull(record);
        _records.Add(record);
    }
}
