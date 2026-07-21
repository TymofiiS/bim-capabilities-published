using FilteringContracts = BIMCapabilities.Contracts.Engines.Family.Filtering;

namespace BIMCapabilities.Engines.Family.Atoms.Filtering;

/// <summary>
/// Filters selected families using combined filter criteria.
/// </summary>
public sealed class FilterFamiliesCombinedAtom : FilteringContracts.IFamilyFilterAtom
{
    public const string FilterAtomId = "family.filter.combined";

    public string AtomId => FilterAtomId;

    public FilteringContracts.FamilyFilterResult Filter(FilteringContracts.FamilyFilterRequest request)
    {
        ArgumentGuard.ThrowIfNull(request);
        ArgumentGuard.ThrowIfNull(request.Criteria);

        var candidates = request.SelectionResult.SelectedFamilies ?? [];
        var filtered = FamilyFilterAtomSupport.FilterCombined(candidates, request.Criteria);
        return FamilyFilterAtomSupport.CreateResult(AtomId, request, filtered);
    }
}
