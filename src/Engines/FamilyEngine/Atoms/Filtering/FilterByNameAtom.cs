using FilteringContracts = BIMCapabilities.Contracts.Engines.Family.Filtering;

namespace BIMCapabilities.Engines.Family.Atoms.Filtering;

/// <summary>
/// Filters selected families by name criteria.
/// </summary>
public sealed class FilterByNameAtom : FilteringContracts.IFamilyFilterAtom
{
    public const string FilterAtomId = "family.filter.by-name";

    public string AtomId => FilterAtomId;

    public FilteringContracts.FamilyFilterResult Filter(FilteringContracts.FamilyFilterRequest request)
    {
        ArgumentGuard.ThrowIfNull(request);
        ArgumentGuard.ThrowIfNull(request.Criteria?.Names);

        var candidates = request.SelectionResult.SelectedFamilies ?? [];
        var filtered = FamilyFilterAtomSupport.FilterByName(candidates, request.Criteria.Names);
        return FamilyFilterAtomSupport.CreateResult(AtomId, request, filtered);
    }
}
