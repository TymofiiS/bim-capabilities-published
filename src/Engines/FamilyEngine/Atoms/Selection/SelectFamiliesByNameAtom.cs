using SelectionContracts = BIMCapabilities.Contracts.Engines.Family.Selection;

namespace BIMCapabilities.Engines.Family.Atoms.Selection;

/// <summary>
/// Selects discovered families by name criteria.
/// </summary>
public sealed class SelectFamiliesByNameAtom : SelectionContracts.IFamilySelectionAtom
{
    public const string SelectionAtomId = "family.selection.by-name";

    public string AtomId => SelectionAtomId;

    public SelectionContracts.FamilySelectionResult Select(SelectionContracts.FamilySelectionRequest request)
    {
        ArgumentGuard.ThrowIfNull(request);
        ArgumentGuard.ThrowIfNull(request.Criteria?.Names);

        var candidates = request.DiscoveryResult.Families ?? [];
        var selected = FamilySelectionAtomSupport.SelectByName(candidates, request.Criteria.Names);
        return FamilySelectionAtomSupport.CreateResult(AtomId, request, selected);
    }
}
