using BIMCapabilities.Contracts.Engines.Family.Discovery;

namespace BIMCapabilities.Contracts.Engines.Family.Selection;

/// <summary>
/// Contract for Family Engine selection atoms.
/// </summary>
public interface IFamilySelectionAtom
{
    string AtomId { get; }

    FamilySelectionResult Select(FamilySelectionRequest request);
}
