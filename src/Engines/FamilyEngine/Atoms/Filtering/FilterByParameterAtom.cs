using FilteringContracts = BIMCapabilities.Contracts.Engines.Family.Filtering;

namespace BIMCapabilities.Engines.Family.Atoms.Filtering;

/// <summary>
/// Filters selected families by parameter criteria.
/// </summary>
public sealed class FilterByParameterAtom : FilteringContracts.IFamilyFilterAtom
{
    public const string FilterAtomId = "family.filter.by-parameter";

    public string AtomId => FilterAtomId;

    public FilteringContracts.FamilyFilterResult Filter(FilteringContracts.FamilyFilterRequest request)
    {
        ArgumentGuard.ThrowIfNull(request);
        ArgumentGuard.ThrowIfNull(request.Criteria?.Parameters);

        var candidates = request.SelectionResult.SelectedFamilies ?? [];
        var filtered = FamilyFilterAtomSupport.FilterByParameter(candidates, request.Criteria.Parameters);
        return FamilyFilterAtomSupport.CreateResult(AtomId, request, filtered);
    }
}
