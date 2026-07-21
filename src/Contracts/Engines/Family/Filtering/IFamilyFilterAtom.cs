using BIMCapabilities.Contracts.Engines.Family.Selection;

namespace BIMCapabilities.Contracts.Engines.Family.Filtering;

/// <summary>
/// Contract for Family Engine filtering atoms.
/// </summary>
public interface IFamilyFilterAtom
{
    string AtomId { get; }

    FamilyFilterResult Filter(FamilyFilterRequest request);
}
