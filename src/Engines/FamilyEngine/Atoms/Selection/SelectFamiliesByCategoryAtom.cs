using SelectionContracts = BIMCapabilities.Contracts.Engines.Family.Selection;

namespace BIMCapabilities.Engines.Family.Atoms.Selection;

/// <summary>
/// Selects discovered families by category criteria.
/// </summary>
public sealed class SelectFamiliesByCategoryAtom : SelectionContracts.IFamilySelectionAtom
{
    public const string SelectionAtomId = "family.selection.by-category";

    public string AtomId => SelectionAtomId;

    public SelectionContracts.FamilySelectionResult Select(SelectionContracts.FamilySelectionRequest request)
    {
        ArgumentGuard.ThrowIfNull(request);
        ArgumentGuard.ThrowIfNull(request.Criteria?.Categories);

        var candidates = request.DiscoveryResult.Families ?? [];
        var selected = FamilySelectionAtomSupport.SelectByCategory(candidates, request.Criteria.Categories);
        return FamilySelectionAtomSupport.CreateResult(AtomId, request, selected);
    }
}
