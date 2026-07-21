using FilteringContracts = BIMCapabilities.Contracts.Engines.Family.Filtering;

namespace BIMCapabilities.Engines.Family.Atoms.Filtering;

/// <summary>
/// Filters selected families by relationship criteria.
/// </summary>
public sealed class FilterByRelationshipAtom : FilteringContracts.IFamilyFilterAtom
{
    public const string FilterAtomId = "family.filter.by-relationship";

    public string AtomId => FilterAtomId;

    public FilteringContracts.FamilyFilterResult Filter(FilteringContracts.FamilyFilterRequest request)
    {
        ArgumentGuard.ThrowIfNull(request);
        ArgumentGuard.ThrowIfNull(request.Criteria?.Relationships);

        var candidates = request.SelectionResult.SelectedFamilies ?? [];
        var filtered = FamilyFilterAtomSupport.FilterByRelationship(candidates, request.Criteria.Relationships);
        return FamilyFilterAtomSupport.CreateResult(AtomId, request, filtered);
    }
}
