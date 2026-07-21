using BIMCapabilities.Contracts.Evidence;

namespace BIMCapabilities.Runtime.Evidence;

/// <summary>
/// Collects runtime evidence records during composition.
/// </summary>
public interface IRuntimeEvidence
{
    EvidenceCollection Collection { get; }

    void Add(EvidenceRecord record);
}
