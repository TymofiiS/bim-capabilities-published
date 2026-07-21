using SelectionContracts = BIMCapabilities.Contracts.Engines.Family.Selection;

namespace BIMCapabilities.Engines.Family.Atoms.Selection;

/// <summary>
/// Selects discovered families using combined selection criteria.
/// </summary>
public sealed class SelectFamiliesCombinedAtom : SelectionContracts.IFamilySelectionAtom
{
    public const string SelectionAtomId = "family.selection.combined";

    public string AtomId => SelectionAtomId;

    public SelectionContracts.FamilySelectionResult Select(SelectionContracts.FamilySelectionRequest request)
    {
        ArgumentGuard.ThrowIfNull(request);
        ArgumentGuard.ThrowIfNull(request.Criteria);

        var candidates = request.DiscoveryResult.Families ?? [];
        var selected = FamilySelectionAtomSupport.SelectCombined(candidates, request.Criteria);
        return FamilySelectionAtomSupport.CreateResult(AtomId, request, selected);
    }
}
