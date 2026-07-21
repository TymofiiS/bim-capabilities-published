using FilteringContracts = BIMCapabilities.Contracts.Engines.Family.Filtering;

namespace BIMCapabilities.Engines.Family.Atoms.Filtering;

/// <summary>
/// Removes selected families that have no family types.
/// </summary>
public sealed class FilterEmptyFamiliesAtom : FilteringContracts.IFamilyFilterAtom
{
    public const string FilterAtomId = "family.filter.empty-families";

    public string AtomId => FilterAtomId;

    public FilteringContracts.FamilyFilterResult Filter(FilteringContracts.FamilyFilterRequest request)
    {
        ArgumentGuard.ThrowIfNull(request);

        var candidates = request.SelectionResult.SelectedFamilies ?? [];
        var filtered = FamilyFilterAtomSupport.FilterEmptyFamilies(candidates);
        return FamilyFilterAtomSupport.CreateResult(AtomId, request, filtered);
    }
}
