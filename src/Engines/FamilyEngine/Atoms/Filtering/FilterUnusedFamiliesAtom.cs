using FilteringContracts = BIMCapabilities.Contracts.Engines.Family.Filtering;

namespace BIMCapabilities.Engines.Family.Atoms.Filtering;

/// <summary>
/// Removes selected families marked as unused.
/// </summary>
public sealed class FilterUnusedFamiliesAtom : FilteringContracts.IFamilyFilterAtom
{
    public const string FilterAtomId = "family.filter.unused-families";

    public string AtomId => FilterAtomId;

    public FilteringContracts.FamilyFilterResult Filter(FilteringContracts.FamilyFilterRequest request)
    {
        ArgumentGuard.ThrowIfNull(request);

        var candidates = request.SelectionResult.SelectedFamilies ?? [];
        var filtered = FamilyFilterAtomSupport.FilterUnusedFamilies(candidates);
        return FamilyFilterAtomSupport.CreateResult(AtomId, request, filtered);
    }
}
