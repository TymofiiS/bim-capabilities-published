using FilteringContracts = BIMCapabilities.Contracts.Engines.Family.Filtering;

namespace BIMCapabilities.Engines.Family.Atoms.Filtering;

/// <summary>
/// Filters selected families by category criteria.
/// </summary>
public sealed class FilterByCategoryAtom : FilteringContracts.IFamilyFilterAtom
{
    public const string FilterAtomId = "family.filter.by-category";

    public string AtomId => FilterAtomId;

    public FilteringContracts.FamilyFilterResult Filter(FilteringContracts.FamilyFilterRequest request)
    {
        ArgumentGuard.ThrowIfNull(request);
        ArgumentGuard.ThrowIfNull(request.Criteria?.Categories);

        var candidates = request.SelectionResult.SelectedFamilies ?? [];
        var filtered = FamilyFilterAtomSupport.FilterByCategory(candidates, request.Criteria.Categories);
        return FamilyFilterAtomSupport.CreateResult(AtomId, request, filtered);
    }
}
